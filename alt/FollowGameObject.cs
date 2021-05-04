using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowGameObject : MonoBehaviour
{
    [SerializeField] Transform gameobjectToFollow;

    private void LateUpdate()
    {
        if (gameobjectToFollow != null)
            transform.position = gameobjectToFollow.position;
    }

}
