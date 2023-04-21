using Epic.OnlineServices.Sessions;
using HarmonyLib;
using JetBrains.Annotations;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        // public static List<OWRigidbody> _gunGrowQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _gunGrowingBody;

        public float _gunNextGrowCheckTime;
        public Key Big;                                                 //Grows all OWRigidbodies on _VanishBlacklist to normal size
        public static bool BigBubbon;
        public Key Small;
        public static bool SmallBubbon;
        public Key Up;
        public static bool UpSibling;
        public Key Down;
        public static bool DownSibling;
        public Key Left;
        public static bool ToParent;
        public Key Right;
        public static bool ToChilds;

        protected bool sceneLoaded;                   //MimickSwapperUpdate uses this to determine when to start running
        //public GameObject _lookingAt;
        //public GameObject _recentTargetObject;

        public static ToolModeSwapper _vanillaSwapper;

        // public static GameObject _sgCamHoldTransformGO;
        // public static GameObject _sgBodyHoldTransformGO;
        // public static GameObject _sgBodyStowTransformGO;
        private GameObject _sgtool_GO_Old;  //MUST BE PUBLIC
        public ScalegunToolClass _theGunToolClass;
        public ScalegunPropClass _sgPropClassMain;
        private StaffSpawner _spawnerOfStaff = new ();

        private Key GunToggle;        //Idk if it'll be more or less work to prevent gun from working while in ship.  guess we'll find out
        public bool toggleGunKey; //whether right-click & other scout-related actions reach the Scalegun instead

        public static ToolMode SGToolmode;


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
        _spawnerOfStaff.SpawnEverything();
        // GOSetup();

        sceneLoaded = true;     //MimicSwapperUpdate can start running now

        _theGunToolClass = Locator.GetPlayerBody().GetComponentInChildren<ScalegunToolClass>(true); // you're not set to "true" here, so you're not searching inactive objects, but also this feels solvable without, idfk

        //hopefully the host _sgtool_GO_Old's inactivity prevents its new ScalegunTool pilot from waking up, or it'll reach for ScalegunPropClass too early
        _vanillaSwapper = Locator.GetToolModeSwapper();    //Should establish _vanillaSwapper as the game's current ToolModeSwapper for future reference

        // _theGunToolClass.enabled = true; //031823_0622: put back after the other one in hopes of addressing a first-time-equip bug  UPDATE: THAT DID NOTHING EITHER            
        //_theGunToolClass.transform.parent.gameObject.SetActive(true);  //will brute force this, cleaarly necessary
        //_sgtool_GO_Old.SetActive(true);
    }
    );
            };
        }

        public override void Configure(IModConfig config)

        //InputLibrary USES ENUMS, FOOD FOR THOTS
        {
            Big = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Big Your Ball"));
            Small = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Small Your Ball"));

            Up = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Up a sibling (default: UpArrow)"));
            Down = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Down a sibling (default: DownArrow)"));
            Left = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("To parent (default: LeftArrow)"));
            Right = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("To childs (default: RightArrow)"));
            GunToggle = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Equip Scalegun"));
        }


        //ToolModeSwapper Mimickry:
        private void Update()       //UNITY EXPLORER REVEALS ALL THE ScalegunTool's methods work fine and even animate well.  IsEquipped returns true.  Something is failing to pull its strings and read its info.
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame;
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;
                UpSibling = Keyboard.current[Up].wasPressedThisFrame;
                DownSibling = Keyboard.current[Down].wasPressedThisFrame;   //THANKS TO Raoul1808 for the tip on where to find the notification stuff!
                ToParent = Keyboard.current[Left].wasPressedThisFrame;
                ToChilds = Keyboard.current[Right].wasPressedThisFrame;
                toggleGunKey = Keyboard.current[GunToggle].wasPressedThisFrame;
            }
            if (sceneLoaded) //The below should probably be part of the ToolClass, but you made such a mess it's gonna take a while to make room
                if (toggleGunKey && OWInput.IsInputMode(InputMode.Character))   //032823_1330: IF PLAYER'S IN EDIT MODE AND HITS Q OR H, THEY SHOULD MOVE IT FROM THEIR CAMERA TO PLAYER_BODY; ONLY CALL Swapper.UnequipTool() WHEN NOT CURRENTLY IN EDIT MODE

                    if (_vanillaSwapper._currentToolMode != SGToolmode)
                        _vanillaSwapper.EquipToolMode(SGToolmode);
                    else
                        _vanillaSwapper.UnequipTool();  //Swapper's UnequipTool method calls EquipToolMode, for reference
                                                        //FOR SOME REASON H IS STILL ACTIVATING THE TOOL CLASS WHEN THE _sgToolGameObject IS INACTIVE, BUT DOESN'T DEACTIVATE IT ON SUBSEQUENT PRESSES.  IDK IF THIS IS ALSO HOW OTHER OBJECTS WORK.
                                                        //UPDATE:  THE SIGNALSCOPE ALSO DOES THIS.  GUESS THAT'S JUST HOW THINGS ARE, NOT A BUG                 
        }

        /// <summary>
        ///AS NOTHING HAS CHANGED ABOUT TOOLMODE SWAPPER BETWEEN WHEN IT WAS WORKING, AND NOW, OBVIOUSLY THE SWAPPER AND PATCH AREN'T THE PROBLEM.
        /// </summary>

        //_currentToolMode WILL ALWAYS REGISTER AS "None" EVEN WHEN ALSO REGISTERING THE CURRENT TOOL.  THIS IS HOW THE BASE GAME DOES THINGS, DON'T QUESTION IT
        //BUT ToolModeSwapper.IsInToolMode returns FALSE when asked if "none" is true while _currentToolMode is ANYTHING?  SOMETHING I'M DOING IS SETTING _currentToolMode to "None" AND THE GAME'S MERCIFULLY IGNORING ME.
        //CHECK BASELINE





        [HarmonyPatch]  //NEVER FORGET THIS AGAIN YOU NUMBSKULL
        public class ScaleGun420PatchClass
        {
            //Owl said i might not even have to patch ToolmodeSwapper.Update?  idk how not but //IT IS DONE, GLORY TO THE EYE.

            [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
            private static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode, ToolModeSwapper __instance)  //instance is for referencing the class currently performing the method you're patching 
            {
                ToolMode scalegunMode = SGToolmode;            //might have to patch swapper regardless to make special exception for leaving editmode
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




