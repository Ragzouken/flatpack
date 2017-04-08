using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class TouchTransformTest : MonoBehaviour 
{
    [SerializeField]
    private Transform touch1, touch2, testObject;

    private bool started;
    private Vector2 prevTouch1, prevTouch2;
    private float baseScale, baseAngle;
    private Vector2 basePosition;

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            touch1.position = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            touch2.position = Input.mousePosition;
        }

        Vector2 nextTouch1 = touch1.position;
        Vector2 nextTouch2 = touch2.position;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            started = true;

            prevTouch1 = nextTouch1;
            prevTouch2 = nextTouch2;

            baseScale = testObject.localScale.z;
            baseAngle = testObject.localEulerAngles.z;
            basePosition = testObject.position;
        }

        if (started)
        {
            Vector2 O = basePosition;
            Vector2 A = prevTouch1;
            Vector2 B = prevTouch2;

            Vector2 a = A - O;
            Vector2 b = B - O;
            Vector2 c = B - A;

            float prevD = (prevTouch2 - prevTouch1).magnitude;
            float nextD = (nextTouch2 - nextTouch1).magnitude;
            float scaleMult = nextD / prevD;

            float prevAngle = Angle(c);
            float nextAngle = Angle(nextTouch2 - nextTouch1);
            float deltaAngle = Mathf.DeltaAngle(prevAngle, nextAngle);

            float prevMagA = a.magnitude;
            float nextMagA = prevMagA * scaleMult;

            Debug.LogFormat("{0} / {1}", scaleMult, deltaAngle);

            Vector2 nexta = Rotate(a * scaleMult, deltaAngle);
            Vector2 nextO = nextTouch1 - nexta;

            //Debug.LogFormat("{0} / {1}", a, nexta);

            testObject.localScale = Vector3.one * baseScale * scaleMult;
            testObject.localEulerAngles = Vector3.forward * (baseAngle + deltaAngle);
            testObject.position = nextO;
        }
        else
        {
            Debug.LogFormat("{0} ({1} / {2})",
                            Angle(Input.mousePosition - touch1.position),
                            Input.mousePosition,
                            touch1.position);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            started = false;
        }
    }

    private static float Angle(Vector2 vector)
    {
        return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    }

    private static Vector2 Rotate(Vector2 vector, float angle)
    {
        float d = vector.magnitude;
        float a = Angle(vector);

        a += angle;
        a *= Mathf.Deg2Rad;

        return new Vector2(d * Mathf.Cos(a), d * Mathf.Sin(a));
    }
}
