#if PROJECT_HAS_UNITASK
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Radish.VContainer
{
    [PublicAPI]
    public interface IGameStateAsync
    {
        UniTask LoadAsync();
        UniTask UnloadAsync();
    }
}
#endif