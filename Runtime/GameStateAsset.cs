using UnityEngine;

namespace Radish.VContainer
{
    /// <summary>
    /// Asset that references a game state object.
    /// </summary>
    [CreateAssetMenu(menuName = RadishConsts.MenuPrefix + "Game State Asset", order = RadishConsts.MenuOrder)]
    public class GameStateAsset : ScriptableObject
    {
        [SerializeField] private SoftAssetReference<GameObject> m_GameStatePrefab;
        public SoftAssetReference<GameObject> gameStatePrefab => m_GameStatePrefab;
    }
}