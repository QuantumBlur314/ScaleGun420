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

        public ToolModeSwapper _vanillaSwapper;

        public GameObject _sgToolGObj;  //MUST BE PUBLIC
        public ScalegunToolClass _theGunToolClass;
        public ScalegunPropClass _sgPropClassMain;

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
        GOSetup();
        sceneLoaded = true;     //MimicSwapperUpdate can start running now
        _vanillaSwapper = Locator.GetToolModeSwapper();    //Should establish _vanillaSwapper as the game's current ToolModeSwapper for future reference
    }
    );
            };
        }

        private void GOSetup()  //does all the object spawning/hierarchies that the base game's creators probably handled better in unity.  idfk.  Does things in such 
        {
            _sgToolGObj = Locator.GetPlayerTransform().CreateChild("SgToolGObj_husk", false);  //031623_0653: spawns an inactive empty SGToolGO as a child of the player.
            _theGunToolClass = _sgToolGObj.AddComponent<ScalegunToolClass>();  //hopefully the host _sgToolGObj's inactivity prevents its new ScalegunTool pilot from waking up, or it'll reach for ScalegunPropClass too early

            _sgPropClassMain = _sgToolGObj.AddComponent<ScalegunPropClass>(); //ScalegunTool declares a PropClass; hopefully not 2late to attach & designate it to the _sgPropGroupject.
            _sgPropClassMain._sgPropGOSelf = _theGunToolClass.transform.InstantiatePrefab("brittlehollow/meshes/props", "BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity" +
                            "/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Prefab_NOM_Staff", false, new Vector3(0.5496f, -1.11f, -0.119f), new Vector3(343.8753f, 200.2473f, 345.2718f));
            _sgToolGObj.AddComponent<GunInterfaces>();
            _theGunToolClass.enabled = true; //031823_0622: put back after the other one in hopes of addressing a first-time-equip bug  UPDATE: THAT DID NOTHING EITHER            

            _sgToolGObj.SetActive(true);
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
                //EYESDRILLHOLES DOES A NULLREF IF CALLED WITHOUT WEARING A SUIT (until you project it to the prop)
            }
        }

        private void MimickSwapperUpdate()
        {
            //_currentToolMode WILL ALWAYS REGISTER AS "None" EVEN WHEN ALSO REGISTERING THE CURRENT TOOL.  THIS IS HOW THE BASE GAME DOES THINGS, DON'T QUESTION IT
            //BUT ToolModeSwapper.IsInToolMode returns FALSE when asked if "none" is true while _currentToolMode is ANYTHING?  SOMETHING I'M DOING IS SETTING _currentToolMode to "None" AND THE GAME'S MERCIFULLY IGNORING ME.
            //CHECK BASELINE


            //if (SmallBubbon) { ModHelper.Console.WriteLine($"_equippedTool is {_vanillaSwapper._equippedTool}"); }
            //if (UpBubbon) { ModHelper.Console.WriteLine($"_nextTool is {_vanillaSwapper._nextTool}"); }

            if (toggleGunKey && OWInput.IsInputMode(InputMode.Character))          //_nextToolMode BECOMES SGToolMode for a SPLIT SECOND then becomes NONE, BUT _nextTOOL NEVER GETS CALLED AT ALL, WHAT ASSIGNS _nextToolMode?
            {
                if (_vanillaSwapper._currentToolMode != SGToolmode)
                {  //FOR SOME REASON H IS STILL ACTIVATING THE TOOL CLASS WHEN THE _sgToolGameObject IS INACTIVE, BUT DOESN'T DEACTIVATE IT ON SUBSEQUENT PRESSES.  IDK IF THIS IS ALSO HOW OTHER OBJECTS WORK.
                   //UPDATE:  THE SIGNALSCOPE ALSO DOES THIS.  GUESS THAT'S JUST HOW THINGS ARE, NOT A BUG
                    _vanillaSwapper.EquipToolMode(SGToolmode);
                }
                else
                {
                    _vanillaSwapper.UnequipTool();  //Swapper's UnequipTool method calls EquipToolMode, for reference
                    if (_vanillaSwapper._nextToolMode != ToolMode.None)
                    {
                        TheLogGoober.WriteLine($"_nextToolMode isn't ToolMode.None, instead it's {_vanillaSwapper._nextToolMode}");
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




