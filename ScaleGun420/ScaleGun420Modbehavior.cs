﻿using Epic.OnlineServices.Sessions;
using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tessellation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace ScaleGun420
{
    public class ScaleGun420Modbehavior : ModBehaviour
    {
        public static List<OWRigidbody> _gunGrowQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _gunGrowingBody;

        public float _gunNextGrowCheckTime;
        public Key Big;                                                 //Grows all OWRigidbodies on _VanishBlacklist to normal size
        public bool BigBubbon;
        public Key Small;
        public bool SmallBubbon;
        public Key Up;
        public bool UpBubbon;
        public Key Down;
        public bool DownBubbon;
        public Key Left;
        public bool LeftBubbon;
        public Key Right;
        public bool RightBubbon;

        private bool sceneLoaded;                   //MimickSwapperUpdate uses this to determine when to start running
        //public GameObject _lookingAt;
        //public GameObject _recentTargetObject;

        private ToolModeSwapper _vanillaSwapper;

        public GameObject _sgToolGameobject;  //MUST BE PUBLIC
        public ScalegunTool _theGunToolClass;

        private Key GunToggle;        //Idk if it'll be more or less work to prevent gun from working while in ship.  guess we'll find out
        private bool toggleGunKey; //whether right-click & other scout-related actions reach the Scalegun instead

        public ToolMode SGToolmode;


        public static ScaleGun420Modbehavior Instance;
        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;
        }
        private void Start()
        {
            sceneLoaded = false;
            SGToolmode = EnumUtils.Create<ToolMode>("Scalegun");  //Enum doesn't get created&destroyed over and over, it's a one-time thing anyway, don't have to put it on sceneload, also don't put the number.  does that itself.

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Events.Unity.FireOnNextUpdate(
    () =>
    {
        ScalegunInit();  //error?
        sceneLoaded = true;     //MimicSwapperUpdate can start running now
        _vanillaSwapper = Locator.GetToolModeSwapper();    //Should establish _vanillaSwapper as the game's current ToolModeSwapper for future reference
    }
    );
            };
        }

        private void ScalegunInit()  
        {  
            _sgToolGameobject = Locator.GetPlayerTransform().CreateChild("SGToolGameObject");  //WORKS
            if (_sgToolGameobject == null) { ModHelper.Console.WriteLine("_sgToolGameobject was null"); }  //NEVER NULL
            _theGunToolClass = _sgToolGameobject.AddComponent<ScalegunTool>();  //031623 update: this has to happen here nvm //ScalegunTool's Awake method should run, defining its local _sgToolGameobject as ScaleGun420's _sgToolGameobject.  america's ass or whatever idc about marvel but sometimes they say funny things
        } //error?  //031623: ScalegunTool is active

        public override void Configure(IModConfig config)

        //InputLibrary USES ENUMS, FOOD FOR THOTS
        {
            Big = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Big Your Ball"));
            Small = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Small Your Ball"));

            Up = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Up a layer (default: UpArrow)"));
            Down = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Down a layer (default: DownArrow)"));
            Left = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Left in layer (default: LeftArrow)"));
            Right = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Right in layer (default: RightArrow)"));
            GunToggle = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Equip Scalegun"));
        }

        private void EyesDrillHoles()          //GameObjects have a SetActive method, the menu uses this, maybe it's single-target?  maybe I don't have to use my own thingus?
        {
            if (BigBubbon && Locator.GetPlayerCamera() != null && _vanillaSwapper.IsInToolMode(SGToolmode))
            {
                Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

                Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
                var retrievedRootObject = hit.collider.transform.GetPath();
                NotificationManager.SharedInstance.PostNotification(
    new NotificationData(NotificationTarget.Player,
       $"{retrievedRootObject} Observed",
       5f, true));
            }
        }

        //ToolModeSwapper Mimickry:
        private void Update()       //UNITY EXPLORER REVEALS ALL THE ScalegunTool's methods work fine and even animate well.  IsEquipped returns true.  Something is failing to pull its strings and read its info.
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame;
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;
                UpBubbon = Keyboard.current[Up].wasPressedThisFrame;
                DownBubbon = Keyboard.current[Down].wasPressedThisFrame;   //THANKS TO Raoul1808 for the tip on where to find the notification stuff!
                LeftBubbon = Keyboard.current[Left].wasPressedThisFrame;
                RightBubbon = Keyboard.current[Right].wasPressedThisFrame;
                toggleGunKey = Keyboard.current[GunToggle].wasPressedThisFrame;
            }
            if (sceneLoaded)
            {
                MimickSwapperUpdate();
                EyesDrillHoles();  //EYESDRILLHOLES DOES A NULLREF IF CALLED WITHOUT WEARING A SUIT (until you project it to the prop)
            }
        }

        private void MimickSwapperUpdate()
        {
            //_currentToolMode WILL ALWAYS REGISTER AS "None" EVEN WHEN ALSO REGISTERING THE CURRENT TOOL.  THIS IS HOW THE BASE GAME DOES THINGS, DON'T QUESTION IT
            //BUT ToolModeSwapper.IsInToolMode returns FALSE when asked if "none" is true while _currentToolMode is ANYTHING?  SOMETHING I'M DOING IS SETTING _currentToolMode to "None" AND THE GAME'S MERCIFULLY IGNORING ME.
            //CHECK BASELINE


            if (SmallBubbon) { ModHelper.Console.WriteLine($"_equippedTool is {_vanillaSwapper._equippedTool}"); }
            if (UpBubbon) { ModHelper.Console.WriteLine($"_nextTool is {_vanillaSwapper._nextTool}"); }

            if (toggleGunKey)          //_nextToolMode BECOMES SGToolMode for a SPLIT SECOND then becomes NONE, BUT _nextTOOL NEVER GETS CALLED AT ALL, WHAT ASSIGNS _nextToolMode?
            {
                if (_vanillaSwapper._currentToolMode != SGToolmode)
                {  //FOR SOME REASON H IS STILL ACTIVATING THE TOOL CLASS WHEN THE _sgToolGameObject IS INACTIVE, BUT DOESN'T DEACTIVATE IT ON SUBSEQUENT PRESSES.  IDK IF THIS IS ALSO HOW OTHER OBJECTS WORK.
                   //UPDATE:  THE SIGNALSCOPE ALSO DOES THIS.  GUESS THAT'S JUST HOW THINGS ARE, NOT A BUG
                    _vanillaSwapper.EquipToolMode(SGToolmode);
                    ModHelper.Console.WriteLine($"Current toolmode: {_vanillaSwapper.GetToolMode()}.  Should be {SGToolmode}. Next toolmode is {_vanillaSwapper._nextToolMode} next tool is {_vanillaSwapper._nextTool}");  //When hitting H while other tool is deployed: stows current tool, "Next Toolmode is Scalegun, next tool is (blank) MAYBE 
                }
                else
                {
                    _vanillaSwapper.UnequipTool();  //Swapper's UnequipTool method calls EquipToolMode, for reference
                    if (_vanillaSwapper._nextToolMode != ToolMode.None)
                    {
                        ModHelper.Console.WriteLine($"_nextToolMode isn't ToolMode.None, instead it's {_vanillaSwapper._nextToolMode}");
                    }
                }

            }
        }


        [HarmonyPatch]  //NEVER FORGET THIS AGAIN YOU NUMBSKULL
        public class ScaleGun420PatchClass
        {
            //Owl said i might not even have to patch ToolmodeSwapper.Update?  idk how not but //IT IS DONE, GLORY TO THE EYE.

            [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
            private static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode, ToolModeSwapper __instance)  //instance is for referencing the class currently performing the method you're patching 
            {
                ToolMode scalegunMode = ScaleGun420Modbehavior.Instance.SGToolmode;
                if (mode != scalegunMode)
                {
                    return true;
                }

                PlayerTool playerTool = ScaleGun420Modbehavior.Instance._theGunToolClass;

                //vv copied from the end of the normal EquipToolMode, HAS TO BE HERE SINCE THE PREFIX OVERRIDES BASE FUNCTIONALITY vv

                if (__instance._equippedTool != playerTool)  //if the ToolModeSwapper's currently-equipped tool isn't the newly-set playerTool,
                {
                    if (__instance._equippedTool != null)    //and isn't null
                    {
                        __instance._equippedTool.UnequipTool();   //unequip the equipped tool,
                        __instance._nextToolMode = mode;     //set the Instance.SGToolmode mode as ToolModeSwapper's _nextToolMode,  THIS PATCH'S BASE-GAME COUNTERPART ISN'T 
                        __instance._nextTool = playerTool;        //THIS IS HOW MIMICKUPDATE GETS ITS _nextTOOL AND WITHOUT THE PATCH FUNCTIONING NOTHING WILL HAPPEN
                        __instance._isSwitchingToolMode = true;
                        return false;                                         //if it's in a prefix that returns Bool, you have to have "return false" not just "return"
                    }
                    playerTool.EquipTool();                                        ////CHECK ScalegunTool.EquipTool(); FIRST
                    __instance._equippedTool = playerTool;
                    __instance._currentToolMode = mode;
                    __instance._nextToolMode = ToolMode.None;
                }
                return false;
            }

        }
    }
}




