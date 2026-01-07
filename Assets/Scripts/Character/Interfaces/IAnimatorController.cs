
using UnityEngine;

public interface IAnimatorController
{
    void SetMovement(Vector2 moveVector, bool isMoving);

    void PlayAttack();

    void PlayHurt();

    void PlayDie();

    void ResetTriggers();

    string GetCurrentAnimationInfo();

    bool IsPlayingAnimation(string stateName, int layerIndex = 0);

    void PlaySkill(string triggerName);


}