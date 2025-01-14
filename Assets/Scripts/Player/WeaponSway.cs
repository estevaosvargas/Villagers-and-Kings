﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    public float SwayAmount = 0.02f;
    public float MaxSwayAmount = 0.03f;
    public float SwaySmooth = 3.0f;
    public float SmoothRotation = 2.0f;
    public float TiltAngle = 25;
    private Vector3 def;

    private void Start()
    {
        def = transform.localPosition;
    }

    private void Update()
    {
        if (MouselockFake.IsLock)
        {
            UpdateSway(0, 0);
        }
        else
        {
            UpdateSway(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }
    }

    public void UpdateSway(float x, float y)
    {
        float factorX = -x * SwayAmount;
        float factorY = -y * SwayAmount;

        factorX = Mathf.Clamp(factorX, -MaxSwayAmount, MaxSwayAmount);
        factorY = Mathf.Clamp(factorY, -MaxSwayAmount, MaxSwayAmount);

        Vector3 final = new Vector3(def.x + factorX, def.y + factorY, def.z);
        transform.localPosition = Vector3.Lerp(transform.localPosition, final, Time.deltaTime * SwaySmooth);

        float tiltAroundZ = -x * TiltAngle;
        float tiltAroundX = y * TiltAngle;
        Quaternion target = Quaternion.Euler(tiltAroundX, tiltAroundZ, 0);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * SmoothRotation);
    }
}