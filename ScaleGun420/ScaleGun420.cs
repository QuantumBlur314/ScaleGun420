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
using Tessellation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace ScaleGun420
{
    public class ScaleGun420 : ModBehaviour
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

        public bool sceneLoaded;                   //MimickSwapperUpdate uses this to determine when to start running
        public GameObject _lookingAt;
        public GameObject _recentTargetObject;

        public ToolModeSwapper _vanillaSwapper;

        public GameObject _sgToolGameobject;
        public ScalegunTool _theGunToolClass;

        public Key GunToggle;        //Idk if it'll be more or less work to prevent gun from working while in ship.  guess we'll find out
        public bool toggleGunKey; //whether right-click & other scout-related actions reach the Scalegun instead

        public ToolMode SGToolmode;


        public static ScaleGun420 Instance;
        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;

        }
        private void Start()
        {
            sceneLoaded = false;

            SGToolmode = EnumUtils.Create<ToolMode>("Scalegun");  //Enum doesn't get created&destroyed over and over, it's a one-time thing anyway, don't have to put it on sceneload, also don't put the number.  does that itself.
            //ALLEGEDLY adds "Scalegun" to the ToolMode enum.  Access it by calling SGToolmode

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
                ModHelper.Events.Unity.FireOnNextUpdate(
    () =>
    {
        ScalegunInit();      //
        sceneLoaded = true;     //MimicSwapperUpdate can start running now
        _vanillaSwapper = Locator.GetToolModeSwapper();    //Should establish _vanillaSwapper as the game's current ToolModeSwapper for future reference
    }
    );
            };
        }


        private void ScalegunInit()
        {
            _sgToolGameobject = new GameObject();    //IS THIS REDUNDANT?
            if (_sgToolGameobject != null)
            { ModHelper.Console.WriteLine("Spawned empty GameObject"); }

            _sgToolGameobject.transform.rotation = Locator.GetPlayerTransform().transform.rotation;
            _sgToolGameobject.transform.position = Locator.GetPlayerTransform().transform.position;
            _sgToolGameobject.transform.parent = Locator.GetPlayerBody().transform;

            _theGunToolClass = _sgToolGameobject.AddComponent<ScalegunTool>();

            _sgToolGameobject.SetActive(false);

        }
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
        public void Update()
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
            EyesDrillHoles();
            if (sceneLoaded)
            {
                if (_vanillaSwapper._equippedTool == _theGunToolClass) { ModHelper.Console.WriteLine("But for just a moment, _theGunTool was equipped.  This fills you with determination."); }
                MimickSwapperUpdate();
                EyesDrillHoles();
            }

        }
        public void MimickSwapperUpdate()
        {

            //ESSENTIAL since 
            if (_vanillaSwapper._isSwitchingToolMode && !_vanillaSwapper._equippedTool.IsEquipped())  //This should be my own special update that only runs to equip/unequip the Scalegun
            {
                ModHelper.Console.WriteLine($"Mimick Ln154: Triggered when _equippedTool {_vanillaSwapper._equippedTool} returned false on PlayerTool-class IsEquipped method");

                _vanillaSwapper._equippedTool = _vanillaSwapper._nextTool;
                ModHelper.Console.WriteLine($"Mimick Ln158: Set _equippedTool to {_vanillaSwapper._equippedTool} from _nextTool (line 157)");  //triggers when stowing, but not equipping, a vanilla tool (aka equipping ToolMode.None) idk why

                _vanillaSwapper._nextTool = null;

                if (_vanillaSwapper._equippedTool != null)
                {
                    _vanillaSwapper._equippedTool.EquipTool();
                    ModHelper.Console.WriteLine($"Mimick: _equippedTool {_vanillaSwapper._equippedTool} wasn't null, so ran _equippedTool's special PlayerTool EquipTool() method. (line 163)");

                }
                _vanillaSwapper._currentToolMode = _vanillaSwapper._nextToolMode;
                _vanillaSwapper._nextToolMode = ToolMode.None;
                _vanillaSwapper._isSwitchingToolMode = false;
            }

            if (SmallBubbon) { ModHelper.Console.WriteLine($"_equippedTool is {_vanillaSwapper._equippedTool}"); }
            if (UpBubbon) { ModHelper.Console.WriteLine($"_nextTool is {_vanillaSwapper._nextTool}"); }  //This is yielding "Next ToolMode is 



            if (toggleGunKey)          //LOADS SAVE WITH STAFF ALREADY EQUIPPED
            {
                if (_vanillaSwapper._currentToolMode != SGToolmode)    //Put the EquipToolMode(SGToolmode) as the FIRST condition and UnequipTool as the SECOND possible action.  idk im desperate at this point
                {
                    _vanillaSwapper.EquipToolMode(SGToolmode);
                    ModHelper.Console.WriteLine($"Current toolmode: {_vanillaSwapper.GetToolMode()}.  Should be {SGToolmode}. Next toolmode is {_vanillaSwapper._nextToolMode}");

                }
                else
                {
                    _vanillaSwapper.UnequipTool();  //Swapper's UnequipTool method CALLS EquipToolMode!!!!  CHECK PATCH
                    if (_vanillaSwapper._nextToolMode != ToolMode.None)
                    {
                        ModHelper.Console.WriteLine($"_nextToolMode isn't ToolMode.None, instead it's {_vanillaSwapper._nextToolMode}");
                    }
                }

            }
        }




        public class ScaleGun420PatchClass
        {
            //in ToolModeSwapper's Awake method, it gets each individual tool by name/type and stores them in its class fields.  This is a problem.  I can't add fields to existing things.

            //Owl said i might not even have to patch ToolmodeSwapper.Update?  idk how not but

            [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
            public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode, ToolModeSwapper __instance)  //instance is for referencing the class currently performing the method you're patching 
            {
                ToolMode scalegunMode = Instance.SGToolmode;
                if (mode != scalegunMode) { return true; }

                PlayerTool playerTool = Instance._theGunToolClass;
                Instance.ModHelper.Console.WriteLine("Reached EquipToolMode prefix at least!");

                //vv copied from the end of the normal EquipToolMode vv

                if (__instance._equippedTool != playerTool)  //if the ToolModeSwapper's currently-equipped tool isn't the newly-set playerTool,
                {
                    if (__instance._equippedTool != null)    //and isn't null
                    {
                        __instance._equippedTool.UnequipTool();   //unequip the equipped tool,
                        __instance._nextToolMode = mode;     //set the Instance.SGToolmode mode as ToolModeSwapper's _nextToolMode,
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




