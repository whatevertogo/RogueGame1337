

using UnityEngine;
[CreateAssetMenu(fileName = "WinLayerReward", menuName = "RogueGame/Game/Win Layer Reward")]
public class GameWinLayerRewardConfig : ScriptableObject
{
    public int layerRewardCoins = 40;
    public bool fullHealOnLayerTransition = true;
}