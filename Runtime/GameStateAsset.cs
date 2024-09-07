using System.Collections.Generic;
using JetBrains.Annotations;
using Radish.AssetManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Radish.VContainer
{
    /// <summary>
    /// Asset that references a game state object.
    /// </summary>
    [PublicAPI]
    [CreateAssetMenu(menuName = RadishConsts.MenuPrefix + "Game State Asset", order = RadishConsts.MenuOrder)]
    public class GameStateAsset : ScriptableObject
    {
        [SerializeField] private SoftAssetReference<GameObject> m_GameStatePrefab;
        public SoftAssetReference<GameObject> gameStatePrefab => m_GameStatePrefab;
        
        [FormerlySerializedAs("m_Extensions")] 
        [SerializeReference] private List<IGameStateExtensionData> m_ExtraData = new();
        public IReadOnlyList<IGameStateExtensionData> extraData => m_ExtraData;

        public bool TryGetExtension<T>(out T extensionData) where T : IGameStateExtensionData
        {
            foreach (var data in m_ExtraData)
            {
                if (data is not T castData) 
                    continue;
                
                extensionData = castData;
                return true;
            }

            extensionData = default;
            return false;
        }
    }
}