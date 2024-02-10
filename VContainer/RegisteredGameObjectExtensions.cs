using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using VContainer;

namespace Radish.VContainer
{
    [PublicAPI]
    public static class RegisteredGameObjectExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterGameObject(this IContainerBuilder builder, RegisteredGameObject go)
        {
            go.Register(builder);
        }
    }
}