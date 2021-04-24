using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Constrains the gameobject to a 2D radius around a given Transform while moving closer to the target transform eg. to position a sound at a water's edge.
/// </summary>
public class ConstrainToRadius : MonoBehaviour
{
    [SerializeField] Transform centreTransform; // transform to orient around
    [SerializeField] Transform targetTransform; // transform to follow
    [SerializeField] float radius;              // distance from centreTransform
    [SerializeField] float elevationOffset;     // +/- elevation

    private Vector3 centrePoint;

    private void Start()
    {
        centrePoint = centreTransform.position;
    }


    private void Update()
    {
        // Follow target smoothly
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetTransform.position, 2f);

        // clamp to a ring around the radius at a position closest to the listener.
        Vector3 diff = newPos - centrePoint;
        Vector3 clampedDiff = ClampMagnitude(diff, radius, radius);
        newPos = centrePoint + clampedDiff;
        transform.position = new Vector3(newPos.x, elevationOffset, newPos.z);

    }

    public static Vector3 ClampMagnitude(Vector3 v, float max, float min)
    {
        // credit to lord of duct: *https://forum.unity.com/threads/clampmagnitude-why-no-minimum.388488/* //

        double sm = v.sqrMagnitude;
        if (sm > (double)max * (double)max) return v.normalized * max;
        else if (sm < (double)min * (double)min) return v.normalized * min;
        return v;
    }

}
