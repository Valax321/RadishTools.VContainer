//#define SAFE_TO_USE_ASYNC_INSTANTIATE
#if PROJECT_HAS_UNITASK
using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Radish.Logging;
using UnityEngine;
using ILogger = Radish.Logging.ILogger;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Radish.VContainer
{
    /// <summary>
    /// Class to manage the loading of game states asynchronously.
    /// </summary>
    [PublicAPI]
    public class GameStateController : MonoBehaviour
    {
        public delegate UniTask StateLoadedCallback(GameStateAsset gameState);
        public delegate void StateLoadedSyncCallback(GameStateAsset gameState);
        
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        
        public event StateLoadedSyncCallback onBeginStateLoad
        {
            add => m_BeginStateLoadCallback += value;
            remove => m_BeginStateLoadCallback -= value;
        }

        public event StateLoadedCallback onStateLoaded
        {
            add => m_StateLoadedCallback += value;
            remove => m_StateLoadedCallback -= value;
        }
        
        public event StateLoadedCallback onStateUnloaded
        {
            add => m_StateUnloadedCallback += value;
            remove => m_StateUnloadedCallback -= value;
        }

        private GameObject m_CurrentState;
        private GameStateAsset m_LastStateAsset;
        private bool m_IsLoadingState;
        private bool m_IsUnloadingState;
        private StateLoadedCallback m_StateLoadedCallback;
        private StateLoadedCallback m_StateUnloadedCallback;
        private StateLoadedSyncCallback m_BeginStateLoadCallback;

        protected virtual async UniTask StateLoaded(GameStateAsset gameState)
        {
            if (m_StateLoadedCallback != null)
            {
                await m_StateLoadedCallback.Invoke(gameState);
            }
        }

        protected virtual async UniTask StateUnloaded(GameStateAsset gameState)
        {
            if (m_StateUnloadedCallback != null)
            {
                await m_StateUnloadedCallback.Invoke(gameState);
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_CurrentState)
            {
                Destroy(m_CurrentState.gameObject);
            }
        }

        /// <summary>
        /// Load a new game state.
        /// </summary>
        /// <param name="gameState">The state to load.</param>
        [PublicAPI]
        public async UniTask LoadStateAsync(GameStateAsset gameState)
        {
            if (!gameState)
                throw new ArgumentNullException(nameof(gameState));
            
            if (m_IsLoadingState)
            {
                Logger.Warn("Already loading a game state");
                return;
            }

            m_IsLoadingState = true;

            m_BeginStateLoadCallback?.Invoke(gameState);
            
            await UnloadCurrentStateAsync();
            Debug.Assert(!m_CurrentState);

            var path = BuildResourcesManifest.instance.GetResourcePathForAsset(gameState.gameStatePrefab);
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error(this, "Failed to get path for {0}", gameState.gameStatePrefab);
                m_IsLoadingState = false;
                return;
            }

            m_LastStateAsset = gameState;

            var statePrefab = await Resources.LoadAsync<GameObject>(path) as GameObject;
            if (!statePrefab)
            {
                Logger.Error(this, "Failed to load game state prefab");
                m_IsLoadingState = false;
                return;
            }
            
            // As of 6000.0.7, InstantiateAsync() will not work with ISerializationCallbackReceiver,
            // so we don't want to use it until this is fixed.
#if SAFE_TO_USE_ASYNC_INSTANTIATE
            m_CurrentState = (await InstantiateAsync(statePrefab))[0];
#else
            m_CurrentState = Instantiate(statePrefab);
#endif
            DontDestroyOnLoad(m_CurrentState.gameObject);
            m_CurrentState.gameObject.name = $"<{statePrefab.name}>";

            for (var i = 0; i < m_CurrentState.GetComponentCount(); ++i)
            {
                var cmp = m_CurrentState.GetComponentAtIndex(i);
                if (cmp is IGameStateAsync asyncState)
                    await asyncState.LoadAsync();
            }

            if (m_CurrentState.TryGetComponent(out GameStateBehaviour gsb))
            {
                await gsb.InitializeComponentsAsync();
            }
            
            await StateLoaded(gameState);

            m_IsLoadingState = false;
        }

        private async UniTask UnloadCurrentStateAsync()
        {
            if (m_IsUnloadingState)
            {
                Logger.Warn("Already unloading a game state");
                return;
            }

            m_IsUnloadingState = true;

            if (m_CurrentState)
            {
                for (var i = 0; i < m_CurrentState.GetComponentCount(); ++i)
                {
                    var cmp = m_CurrentState.GetComponentAtIndex(i);
                    if (cmp is IGameStateAsync asyncState)
                        await asyncState.UnloadAsync();
                }
                
                if (m_CurrentState.TryGetComponent(out GameStateBehaviour gsb))
                {
                    await gsb.DestroyComponentsAsync();
                }
                
                Destroy(m_CurrentState);
            }
            m_CurrentState = null;

            await Resources.UnloadUnusedAssets();

            await StateUnloaded(m_LastStateAsset);

            m_IsUnloadingState = false;
        }
    }
}
#endif