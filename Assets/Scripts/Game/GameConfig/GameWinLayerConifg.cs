

using UnityEngine;
[CreateAssetMenu(fileName = "GameWinLayerRewardConfig", menuName = "Game/Win Layer Reward Config")]
public class GameWinLayerRewardConfig : ScriptableObject
{
    public int layerRewardCoins = 40;
    public bool fullHealOnLayerTransition = true;
}