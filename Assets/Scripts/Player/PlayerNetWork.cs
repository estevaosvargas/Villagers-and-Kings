﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNetWork : EntityLife
{
    public NetWorkView Net;
    public bool Render_IsVisible { get; private set; }
    public bool IsMyPlayerNet { get; private set; }
    public List<MonoBehaviour> ScriptToRemove = new List<MonoBehaviour>();
    public List<GameObject> ObjectToDisable = new List<GameObject>();
    public PlayerNetStats NetStats;
    public Animator Anim;

    void Start()
    {
        Anim = GetComponent<Animator>();
        if (Net.isMine)
        {
            foreach (var item in ScriptToRemove)
            {
                item.enabled = true;
            }
            IsMyPlayerNet = true;
        }
        else
        {
            foreach (var item in ScriptToRemove)
            {
                Destroy(item);
            }
            IsMyPlayerNet = false;
        }
    }

    void Update()
    {
        if (Render_IsVisible)
        {
            if (Game.GameManager.MultiPlayer)
            {
                if (!IsMyPlayerNet)// if is another player net
                {

                }
            }
        }
    }

    private void OnBecameVisible()
    {
        Render_IsVisible = true;
    }

    public void OnBecameInvisible()
    {
        Render_IsVisible = false;
    }

    public void FootPrintRight()
    {
        //AUDIOSOURCE.PlayOneShot(Game.AudioManager.GetFootSound(playerNetWork.NetStats.CurrentTile));
        //FootPArticle.Emit(FootParticleCount);
    }

    public void FootPrintLeft()
    {
        //AUDIOSOURCE.PlayOneShot(Game.AudioManager.GetFootSound(playerNetWork.NetStats.CurrentTile));
        //FootPArticle.Emit(FootParticleCount);
    }

    [RPC]
    void UpdatePosition(Vector3 pos)
    {
        transform.position = pos;
    }

    [RPC]
    void RPC_Syncplayervalues(float angle, int side, bool iswalking)
    {
        if (Render_IsVisible)// if this player is showing on the camera, the can do any update
        {
            int realside = 0;//is gone show the real direction of player in graus EX: 180, 90, 360 etc.

            switch (side)
            {
                case 0:
                    realside = 0;
                    break;
                case 1:
                    realside = 180;
                    break;
                case 2:
                    realside = 90;
                    break;
                case 3:
                    realside = -90;
                    break;
            } //Decode side of player, with this we can save transfer data

            Anim.SetInteger("Walk", iswalking ? 1 : 0);
            Anim.SetFloat("X", realside);

            NetStats.angle = angle;
            NetStats.Side = side;
            NetStats.walking = iswalking;
        }
    }
}