using System;
using System.Collections.Generic;
using JetBrains.Annotations;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Radish.VContainer
{
    /// <summary>
    /// Allows for registering gameobject components with VContainer without having to hardcode different prefabs
    /// and instantiate them manually. This allows for extending games without extensive code changes and keeps things encapsulated better.
    /// </summary>
    [Serializable]
    public sealed class RegisteredGameObject
    {
        [SerializeField] private string m_Name = "New GameObject";
        
        #if ODIN_INSPECTOR
        [AssetsOnly]
        #endif
        [SerializeField] private GameObject m_Prefab;
        [SerializeField] private bool m_DontDestroyOnLoad;
        
        [SerializedTypeSettings(typeof(MonoBehaviour))]
        [SerializeField] private List<SerializedType> m_ComponentTypesToRegister = new();

        /// <summary>
        /// Registers this GameObject with the specified container builder.
        /// </summary>
        /// <param name="builder">The builder to register with.</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="builder"/> is null.</exception>
        /// <exception cref="UnassignedReferenceException">Thrown when the prefab has not been assigned.</exception>
        [PublicAPI]
        public void Register(IContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (!m_Prefab)
                throw new UnassignedReferenceException("Prefab not assigned");
            
            builder.RegisterNewPrefabInstanceComponents(m_Prefab, builderFunc: (_, o) =>
            {
                if (m_DontDestroyOnLoad)
                    UnityEngine.Object.DontDestroyOnLoad(o);

                o.name = $"[{m_Name}]";

                foreach (var type in m_ComponentTypesToRegister)
                {
                    if (o.TryGetComponent(type.type, out var cmp))
                    {
                        builder.RegisterBuildCallback(container =>
                        {
                            container.Resolve(type.type);
                        });
                        builder.RegisterComponent(cmp)
                            .As(type.type);
                    }
                }
            });
        }
    }
}