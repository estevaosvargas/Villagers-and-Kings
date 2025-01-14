﻿using Lidgren.Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class EntityPlayer : EntityLife
{
    public Animator Anim;
    public Animator AnimBob;
    public Animator AnimHand;
    public Camera camera;
    public CharacterController characterController;
    public AudioSource audioSource;
    public Inventory Inve;
    public LifeStatus Status;
    public Transform HandRoot;
    public PlayerNetStats NetStats;
    public ParticleSystem FootPArticle;
    public UniversalStatusPlayer UniPlayer;
    public List<MonoBehaviour> ScriptToRemove = new List<MonoBehaviour>();
    public List<GameObject> ObjectToDisable = new List<GameObject>();
    public Transform Vector;
    public Animator AttackSword;
    public Block block;

    [Space(4)]
    public bool IsVisible = false;
    public bool IsMe = false;
    public int FootParticleCount = 1;

    private Vector3 lastposition;
    private int LastPostitionIntX;
    private int LastPostitionIntZ;
    private float timestep;

    private bool IsOnDamageArea;
    public int DamageRate;

    public float StatusUpdateRate = 1;
    private float Statustimestep;
    private Vector3 velocity;
    private Vector3 moveVector;
    private Vector3 inputDirection;

    void Start()
    {
        Net = GetComponent<NetWorkView>();
        Anim = GetComponent<Animator>();
        Inve = GetComponent<Inventory>();
        characterController = GetComponent<CharacterController>();

        IsMe = Net.isMine;
        if (Net.isMine)
        {
            Game.entityPlayer = this;

            IsAlive = true;

            Game.MenuManager.LifeBar.RefreshBar(HP);
            Game.MenuManager.EnergyBar.RefreshBar(Status.Energy);

            block = Game.World.GetTileAt(transform.position.x,  transform.position.z);
        }
        else
        {
            foreach (var item in ScriptToRemove)
            {
                Destroy(item);
            }
        }
    }

    void UpdateOnMove()
    {
        if (DarckNet.Network.IsClient)
        {
            Net.RPC("UpdatePosition", DarckNet.RPCMode.AllNoOwner, new Vector3(transform.position.x, transform.position.y, transform.position.z));
        }
    }

    void UpdateOnMoveInt()
    {
        if (Game.World)
        {
            var main = FootPArticle.main;
            Game.World.CheckViewDistance();

            block = Game.World.GetTileAt(transform.position.x, transform.position.z);

            if (block == null)
            {
                NetStats.CurrentBlock = block;
                NetStats.CurrentBiome = block.TileBiome;

                if (block.Type == TypeBlock.Grass)
                {
                    main.startColor = Color.green;
                }
                else if (block.Type == TypeBlock.Dirt)
                {
                    main.startColor = Color.magenta;
                }
                else if (block.Type == TypeBlock.DirtRoad)
                {
                    main.startColor = Color.magenta;
                }
                else if (block.Type == TypeBlock.Sand || block.Type == TypeBlock.BeachSand)
                {
                    main.startColor = Color.yellow;
                }
                else if (block.Type == TypeBlock.Rock || block.Type == TypeBlock.RockGround)
                {
                    main.startColor = Color.gray;
                }
                else if (block.Type == TypeBlock.Snow)
                {
                    main.startColor = Color.white;
                }
            }
        }
        
        /*if (MiniMapManager.manager)
        {
            MiniMapManager.manager.UpdateMap();
        }*/
    }

    public Vector3 Move(Vector3 directionVector)
    {
        inputDirection = directionVector;
        if (directionVector != Vector3.zero)
        {
            var directionLength = directionVector.magnitude;
            directionVector = directionVector / directionLength;
            directionLength = Mathf.Min(1, directionLength);
            directionLength = directionLength * directionLength;
            directionVector = directionVector * directionLength;
        }

        Quaternion rotation = transform.rotation;

        Vector3 angle = rotation.eulerAngles;
        angle.x = 0;
        angle.z = 0;
        rotation.eulerAngles = angle;
        return rotation * directionVector;
    }

    private Vector3 prevPos;

    void FixedUpdate()
    {
        velocity = (transform.position - prevPos) / Time.deltaTime;
        prevPos = transform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            characterController.enabled = true;
        }

        if (IsVisible)//Do the Client Update, and Server.
        {
            if (DarckNet.Network.IsClient || Game.GameManager.SinglePlayer)///Client Update
            {
                if (IsMe)//check if this player is me.
                {
                    #region MyPlayerFunctions
                    if (Game.GameManager.MultiPlayer || Game.GameManager.SinglePlayer)
                    {
                        UpdateplaceCursorBlocks();
                       
                        if (Input.GetKeyDown(KeyCode.Mouse0))
                        {
                            Block blockcurrent =  Game.World.GetTileAt(highlightBlock.x,  highlightBlock.z);
                            
                            if (blockcurrent != null)
                            {
                                blockcurrent.RemoveBlock();
                            }
                        }

                        if (Input.GetKeyDown(KeyCode.Mouse1))
                        {
                            Block blockcurrent = Game.World.GetTileAt(highlightBlock.x, highlightBlock.z);

                            if (blockcurrent != null)
                            {
                                blockcurrent.PlaceBlock();
                            }
                        }

                        moveVector = new Vector3(Input.GetAxis("Horizontal"),0, Input.GetAxis("Vertical"));

                        

                        var fwdDotProduct = Vector3.Dot(transform.forward, velocity);
                        var upDotProduct = Vector3.Dot(transform.up, velocity);
                        var rightDotProduct = Vector3.Dot(transform.right, velocity);
             
                        Vector3 velocityVector = new Vector3(rightDotProduct, upDotProduct, fwdDotProduct);

                        Status.UpdateStatus();

                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            AnimBob.speed = 1.5f;
                        }
                        else
                        {
                            AnimBob.speed = 1f;
                        }

                        //PlayerMovement();

                        if (Time.time > Statustimestep + StatusUpdateRate)
                        {
                            if (Status.Energy < Status.MaxEnergy)
                            {
                                Status.Energy += 10;
                                Game.MenuManager.EnergyBar.RefreshBar(Status.Energy);
                            }
                            Statustimestep = Time.time;
                        }

                        if (IsOnDamageArea)
                        {
                            if (Time.time > timestep + DamageRate)
                            {
                                DoDamage(null, 5, Game.GameManager.Player.UserID, true);
                                timestep = Time.time;
                            }
                        }
                        /*if (enableGravity)
                        {
                            if (charbody.isGrounded == false)
                            {
                                moveVector += Physics.gravity;
                            }
                        }*/

                        //charbody.Move(velocity);
                        if (velocity.magnitude > 0)
                        {
                            Anim.SetInteger("Walk", 1);
                            NetStats.walking = true;
                            Anim.SetFloat("X", Input.GetAxis("Horizontal"));
                            Anim.SetFloat("Y", Input.GetAxis("Vertical"));
                            AnimBob.SetBool("AnimBlend", true);
                        }
                        else
                        {
                            Anim.SetInteger("Walk", 0);
                            FootPArticle.Stop();
                            NetStats.walking = false;
                            AnimBob.SetBool("AnimBlend", false);
                        }

                        #region UpDateOnMove
                        if (lastposition != transform.position)
                        {
                            UpdateOnMove();
                        }
                        lastposition = transform.position;

                        if (LastPostitionIntX != (int)transform.position.x || LastPostitionIntZ != (int)transform.position.z)
                        {
                            UpdateOnMoveInt();
                        }
                        LastPostitionIntX = (int)transform.position.x;
                        LastPostitionIntZ = (int)transform.position.z;
                        #endregion
                    }
                    #endregion
                }
                else
                {

                }
            }
            else///Server Update
            {

            }
        }
    }

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Vector3 highlightBlock;
    public Vector3 placeBlock;

    private void UpdateplaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = camera.transform.position + (camera.transform.forward * step);

            Block block = Game.World.GetTileAt(pos.x, pos.z);

            if (block != null)
            {
                if (block.Type != TypeBlock.Air || block.typego != TakeGO.empty)
                {
                    highlightBlock = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                    placeBlock = lastPos;

                    Game.GameManager.blockHightLight.gameObject.SetActive(true);
                    Game.GameManager.blockHightLight.position = highlightBlock + new Vector3(0.5f, 0.5f, 0.5f);
                    //Game.GameManager.blockHightLight.localScale = new Vector3(Game.GameManager.blockHightLight.localScale.x, Get.GetLayerVerticesPlusY(block.LayerLevel), Game.GameManager.blockHightLight.localScale.z);
                    return;

                }
            }
            else
            {
                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;

        }
        Game.GameManager.blockHightLight.gameObject.SetActive(false);
    }

    public bool IsWalking()
    {
        return NetStats.walking;
    }
    private void Step()
    {
        audioSource.PlayOneShot(Game.AudioManager.GetFootSound(block.Type));
        FootPArticle.Emit(FootParticleCount);
    }

    private void OnBecameVisible()
    {
        IsVisible = true;
        Anim.enabled = true;
        Game.Entity_viewing.Add(this);
    }

    private void OnBecameInvisible()
    {
        IsVisible = false;
        Anim.enabled = false;
        Game.Entity_viewing.Remove(this);
    }

    void OnTriggerEnter(Collider collision)
    {
        if (IsMe || DarckNet.Network.IsServer)
        {
            if (collision.tag == "ItemDrop")
            {
                collision.GetComponent<ItemDrop>().GetThisItem(Inve);
            }
            else if (collision.tag == "City")
            {
                Game.MenuManager.PopUpName("My Homes - City");
            }
            else if (collision.tag == "DamageArea")
            {
                IsOnDamageArea = true;
            }
        }
    }

    void OnTriggerExit(Collider collision)
    {
        if (IsMe || DarckNet.Network.IsServer)
        {
            if (collision.tag == "ItemDrop")
            {

            }
            else if (collision.tag == "DamageArea")
            {
                IsOnDamageArea = false;
            }
        }
    }

    public override void OnDead()
    {
        DarckNet.Network.Destroy(this.gameObject);
        Inve.DeletSave();
        Game.MapManager.PlayerDead(this);
        base.OnDead();
    }

    private void OnDestroy()
    {
        if (IsAlive)
        {
            if (IsMe)
            {
                Inve.Save();
                Debug.Log("Saved Your Player!");
            }
        }
    }

    public override void FinishDamage()
    {
        Game.MenuManager.LifeBar.RefreshBar(HP);
        base.FinishDamage();
    }

    public override void FinishCura()
    {
        Game.MenuManager.LifeBar.RefreshBar(HP);
        base.FinishCura();
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }

#region RPCs
[RPC]
    void UpdatePosition(Vector3 pos)
    {
        transform.position = pos;
    }
#endregion
}

[System.Serializable]
public class LifeStatus
{
    [Header("CharCracterstic")]
    public CharRace Race;
    [Header("CharStatus")]
    public int Energy = 100;
    public int MaxEnergy = 100;
    public float Mana = 80;
    [Header("SocialStatus")]
    public int Age = 19;
    public int Friends = 0;
    public int SocialFactor = 100;
    public List<SkillStruc> SkillsList = new List<SkillStruc>();
    public List<WepoSkillStruc> WPSkillsList = new List<WepoSkillStruc>();
    public float XP_PLAYER = 0;

    public void AddSkillDefault(Skills skill)
    {
        switch (skill)
        {
            case Skills.Build:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Cartography:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Combat:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Cook:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Fishing:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Politic:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Survival:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            case Skills.Wirter:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
            default:
                SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
                break;
        }
    }

    public void CalculateXp(Skills skill, float Xpadd)
    {
        SkillStruc CurrentSkill = null;

        foreach (var skillitem in SkillsList)
        {
            if (skillitem.Type == skill)
            {
                CurrentSkill = skillitem;
            }
        }

        if (CurrentSkill == null)
        {
            SkillsList.Add(new SkillStruc(skill, 0, 0, 100, 100));
            CurrentSkill = SkillsList[SkillsList.Count -1];
        }

        if (!CurrentSkill.LevelMax)
        {
            CurrentSkill.SkillXp += Xpadd;

            if (CurrentSkill.SkillXp >= CurrentSkill.MaxSkillXp)
            {
                if (CurrentSkill.SkillLevel >= CurrentSkill.MaxSkillLevel)
                {
                    CurrentSkill.LevelMax = true;
                }
                else
                {
                    CurrentSkill.SkillLevel += 1;
                }

                CurrentSkill.SkillXp = 0;
            }
            StatusWindow.Instance.Refresh();
        }
    }

    public void CalculateXp(ItemData Item, float Xpadd)
    {
        if (Item.ITEMTYPE == ItemType.Weapon || Item.ITEMTYPE == ItemType.Tools)
        {
            WepoSkillStruc CurrentSkill = null;

            foreach (var skillitem in WPSkillsList)
            {
                if (skillitem.Itemid == Item.Index)
                {
                    CurrentSkill = skillitem;
                }
            }

            if (CurrentSkill == null)
            {
                WPSkillsList.Add(new WepoSkillStruc(Item.Index, 0, 0, 100, 100));
                CurrentSkill = WPSkillsList[WPSkillsList.Count - 1];
            }

            if (!CurrentSkill.LevelMax)
            {
                CurrentSkill.SkillXp += Xpadd;

                if (CurrentSkill.SkillXp >= CurrentSkill.MaxSkillXp)
                {
                    if (CurrentSkill.SkillLevel >= CurrentSkill.MaxSkillLevel)
                    {
                        CurrentSkill.LevelMax = true;
                    }
                    else
                    {
                        CurrentSkill.SkillLevel += 1;
                    }
                    CurrentSkill.SkillXp = 0;
                }

                StatusWindow.Instance.Refresh();
            }
        }
    }

    public void UpdateStatus()
    {
        if (Input.GetKey(KeyCode.K))
        {
            CalculateXp((Skills)Random.Range(0, 11), 1);
            CalculateXp(ItemManager.Instance.GetItem(Random.Range(0, 7)), 1);
        }

        if (Input.GetKey(KeyCode.L))
        {
            CalculateXp((Skills)Random.Range(0, 11), -1);
            CalculateXp(ItemManager.Instance.GetItem(Random.Range(0, 7)), -1);
        }
    }

    public void Eat()
    {

    }

    public void Drink()
    {

    }

    public void DrinkMana()
    {

    }
}

[System.Serializable]
public class SkillStruc
{
    public Skills Type;
    public int SkillLevel;
    public float SkillXp;
    public int MaxSkillXp;
    public int MaxSkillLevel;
    public bool LevelMax = false;

    public SkillStruc(Skills type,int skilllevel, float skillxp, int maxskillxp, int maxskilllevel)
    {
        Type = type;
        SkillLevel = skilllevel;
        SkillXp = skillxp;
        MaxSkillXp = maxskillxp;
        MaxSkillLevel = maxskilllevel;
    }
}

[System.Serializable]
public class WepoSkillStruc
{
    public int Itemid;
    public int SkillLevel;
    public float SkillXp;
    public int MaxSkillXp;
    public int MaxSkillLevel;
    public bool LevelMax = false;

    public WepoSkillStruc(int itemid, int skilllevel, float skillxp, int maxskillxp, int maxskilllevel)
    {
        Itemid = itemid;
        SkillLevel = skilllevel;
        SkillXp = skillxp;
        MaxSkillXp = maxskillxp;
        MaxSkillLevel = maxskilllevel;
    }
}

[System.Serializable]
public class UniversalStatusPlayer
{
    public AudioSource AUDIOSOURCE;
}

[System.Serializable]
public class PlayerNetStats
{
    public bool walking = false;
    public bool swiming = false;
    public Block CurrentBlock;
    public BiomeType CurrentBiome;
}

public enum Skills : byte
{
    none, Combat, Survival, Cook, Fishing, Build, Politic, Wirter, Cartography, Mage, Baker, Merchant
}

public enum Language : byte
{
    none, StrageLanguage, OldHumanLanguage, HumanLanguage, OrcsLanguage, ElfLanguage
}