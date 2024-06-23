using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Radish.VContainer
{
    /// <summary>
    /// Application lifecycle manager.
    /// </summary>
    [PublicAPI]
    public class ApplicationController : LifetimeScope
    {
        [Header("Application Controller")]
        [SerializedTypeSettings(typeof(IStartable), typeof(IAsyncStartable))]
        [SerializeField] private SerializedType m_EntryPointType;
        [SerializeField] private bool m_DontDestroyOnLoad = true;
        [SerializeField] private List<RegisteredGameObject> m_ApplicationComponents = new();
        [SerializeField] private List<ScriptableObject> m_GlobalData = new();

        public Type entryPointType => m_EntryPointType.type;
        public bool dontDestroyOnLoad => m_DontDestroyOnLoad;
        public IReadOnlyList<RegisteredGameObject> applicationComponents => m_ApplicationComponents;
        public IReadOnlyList<ScriptableObject> globalData => m_GlobalData;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            
            foreach (var cmp in m_ApplicationComponents)
                cmp.Register(builder);

            foreach (var data in m_GlobalData)
                builder.RegisterInstance(data).As(data.GetType());

            if (m_EntryPointType.type != null)
            {
                // This is what RegisterEntryPoint does internally
                EntryPointsBuilder.EnsureDispatcherRegistered(builder);
                builder.Register(m_EntryPointType.type, Lifetime.Singleton).AsImplementedInterfaces();
            }
        }

        protected override void Awake()
        {
            if (m_DontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            
            base.Awake();
        }
    }
}