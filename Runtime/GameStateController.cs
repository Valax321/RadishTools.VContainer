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
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private GameObject m_CurrentState;
        private GameStateAsset m_LastStateAsset;
        private bool m_IsLoadingState;
        private bool m_IsUnloadingState;

        protected virtual async UniTask OnStateLoaded(GameStateAsset gameState)
        {
        }

        protected virtual async UniTask OnStateUnloaded(GameStateAsset gameState)
        {
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
            
            await UnloadCurrentStateAsync();
            Debug.Assert(!m_CurrentState);

            var path = BuildResourcesManifest.instance.GetResourcePathForAsset(gameState.gameStatePrefab);
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("Failed to get path for {0}", gameState.gameStatePrefab);
                return;
            }

            m_LastStateAsset = gameState;

            var statePrefab = await Resources.LoadAsync<GameObject>(path) as GameObject;
            m_CurrentState = (await InstantiateAsync(statePrefab))[0];
            DontDestroyOnLoad(m_CurrentState.gameObject);

            for (var i = 0; i < m_CurrentState.GetComponentCount(); ++i)
            {
                var cmp = m_CurrentState.GetComponentAtIndex(i);
                if (cmp is IGameStateAsync asyncState)
                    await asyncState.LoadAsync();
            }
            
            await OnStateLoaded(gameState);

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
            
            for (var i = 0; i < m_CurrentState.GetComponentCount(); ++i)
            {
                var cmp = m_CurrentState.GetComponentAtIndex(i);
                if (cmp is IGameStateAsync asyncState)
                    await asyncState.UnloadAsync();
            }
            
            if (m_CurrentState)
                Destroy(m_CurrentState);
            m_CurrentState = null;

            await Resources.UnloadUnusedAssets();

            await OnStateUnloaded(m_LastStateAsset);

            m_IsUnloadingState = false;
        }
    }
}
#endif