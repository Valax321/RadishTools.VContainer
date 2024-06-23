using System.Collections.Generic;
#if PROJECT_HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif
using JetBrains.Annotations;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Radish.VContainer
{
    [PublicAPI]
    public abstract class GameStateBehaviour : LifetimeScope
    {
        [Header("Game State")]
        [SerializeField] private List<RegisteredGameObject> m_Components = new();
        
        public virtual bool persistent => false;

        private static GameObject s_ActiveStateObject;

        private List<Component> m_RegisteredComponents = new();

        [PublicAPI]
        public static void InjectIntoCurrentState(GameObject instance)
        {
            if (s_ActiveStateObject)
            {
                if (s_ActiveStateObject.TryGetComponent<GameStateBehaviour>(out var b))
                {
                    b.Container.InjectGameObject(instance);
                }
            }
        }

        [PublicAPI]
        public static LifetimeScope GetCurrentScope()
        {
            if (s_ActiveStateObject)
            {
                if (s_ActiveStateObject.TryGetComponent<GameStateBehaviour>(out var b))
                {
                    return b;
                }
            }

            return null;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            
            foreach (var cmp in m_Components)
                m_RegisteredComponents.AddRange(cmp.Register(builder));
        }

#if PROJECT_HAS_UNITASK
        internal async UniTask InitializeComponentsAsync()
        {
            foreach (var cmp in m_RegisteredComponents)
            {
                if (cmp is IRegisteredComponentAsync asyncCmp)
                    await asyncCmp.OnCreateAsync();
            }
        }
        
        internal async UniTask DestroyComponentsAsync()
        {
            foreach (var cmp in m_RegisteredComponents)
            {
                if (cmp is IRegisteredComponentAsync asyncCmp)
                    await asyncCmp.OnDestroyAsync();
            }
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            
            if (Parent != null)
                Parent.Container.Inject(this);
        }

        protected virtual void Start()
        {
            if (s_ActiveStateObject && s_ActiveStateObject != gameObject)
            {
                var prevState = s_ActiveStateObject.GetComponent<GameStateBehaviour>();
                if (prevState.persistent && prevState.GetType() == GetType())
                {
                    Destroy(gameObject);
                    return;
                }
                
                Destroy(s_ActiveStateObject);
            }

            s_ActiveStateObject = gameObject;
            if (persistent)
                DontDestroyOnLoad(gameObject);
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!persistent)
                s_ActiveStateObject = null;
        }
    }
}