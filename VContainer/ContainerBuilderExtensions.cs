using System;
using JetBrains.Annotations;
using Radish;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

[PublicAPI]
// ReSharper disable once CheckNamespace
public static class ContainerBuilderExtensions
{
    public static RegistrationBuilder RegisterEventBus<T>(this IContainerBuilder builder) where T : struct, IEventBusMessage
    {
        return builder.RegisterInstance(new EventBus<T>());
    }

    public static void RegisterNewPrefabInstanceComponents(this IContainerBuilder builder, GameObject prefab,
        Action<ComponentsBuilder, GameObject> builderFunc)
    {
        var instance = Object.Instantiate(prefab);
        builderFunc(new ComponentsBuilder(builder, instance.transform), instance);
    }
}