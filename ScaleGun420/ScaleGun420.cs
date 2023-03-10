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

        public GameObject _lookingAt;
        public GameObject _recentTargetObject;
        public GameObject _vesselThroughWhichIExertMyWill;
        public ScalegunTool _theGunTool;

        public Key GunToggle;        //Idk if it'll be more or less work to prevent gun from working while in ship.  guess we'll find out
        public bool toggleGunKey; //whether right-click & other scout-related actions reach the Scalegun instead
        public bool _gunIsEquipped;

        public ToolModeSwapper _pHSWAPPER;
        public ToolMode SGToolmode;


        public static ScaleGun420 Instance;
        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;

        }
        private void Start()
        {
            ModHelper.Console.WriteLine($"My mod {nameof(ScaleGun420)} is loaded!", MessageType.Success);
            // INewHorizons NewHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");

            SGToolmode = EnumUtils.Create<ToolMode>("Scalegun = 10");  //Enum doesn't get created&destroyed over and over, it's a one-time thing anyway, don't have to put it on sceneload

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
                ModHelper.Events.Unity.FireOnNextUpdate(
    () =>
    {
        ScalegunInit();


    }
);
            };
            //Local position: 0.5496 -1.121 -0.119
            //Rotation 343.8753 200.2473 345.2718
            // GlobalMessenger<ProbeLauncher>.AddListener("ProbeLauncherEquipped", new Callback<ProbeLauncher>(this.OnProbeLauncherEquipped)); //Listens for ProbeLauncher events

        }
        //private void booty()

        //  instancedStaff.AddComponent<ScalegunTool>();
        //   _theGunTool = instancedStaff.GetComponent<ScalegunTool>();    //DO I STILL NEED TO DO THIS NOW THE STAFF IS ALREADY INSIDE ScalegunTool'S CLASS?
        // instancedStaff.SetActive(true);

        private void ScalegunInit()
        {
            _vesselThroughWhichIExertMyWill = new GameObject();
            if (_vesselThroughWhichIExertMyWill != null)
            { ModHelper.Console.WriteLine("Spawned empty GameObject"); }
            //_vesselThroughWhichIExertMyWill.transform.SetParent(Locator.GetPlayerTransform());

            _vesselThroughWhichIExertMyWill.transform.rotation = Locator.GetPlayerTransform().transform.rotation;
            _vesselThroughWhichIExertMyWill.transform.position = Locator.GetPlayerTransform().transform.position;
            _vesselThroughWhichIExertMyWill.transform.parent = Locator.GetPlayerBody().transform;


            if (_vesselThroughWhichIExertMyWill.transform != null)
            {
                ModHelper.Console.WriteLine("Got Transforms");
            }
            _vesselThroughWhichIExertMyWill.AddComponent<ScalegunTool>();
            if (_vesselThroughWhichIExertMyWill.GetComponent<ScalegunTool>())
            { ModHelper.Console.WriteLine("Added ScalegunTool component"); }
            _theGunTool = _vesselThroughWhichIExertMyWill.GetRequiredComponentInChildren<ScalegunTool>();  //Same as how the NomaiTranslator tool's Awake() method declares NomaiTranslatorProp
            _vesselThroughWhichIExertMyWill.SetActive(true);
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
        //Put the scalegun away when getting into pilot seat

        private void ToggleScaleGun()    //need to patch ProbeLauncher.AllowInput to account for Scalegun. //Wait, won't that mess with ScalegunTool itself???//Probably also need to patch ProbeLauncher.Update
        {
            //if player is currently wielding another tool 
            var toolModeSwapper = PlayerBody.FindObjectOfType<ToolModeSwapper>();
            if (toolModeSwapper._isSwitchingToolMode && !toolModeSwapper._equippedTool.IsEquipped())
            {
                toolModeSwapper._equippedTool = toolModeSwapper._nextTool;
                toolModeSwapper._nextTool = null;
                if (toolModeSwapper._equippedTool != null)
                {
                    toolModeSwapper._equippedTool.EquipTool();
                }
                toolModeSwapper._currentToolMode = toolModeSwapper._nextToolMode;
                toolModeSwapper._nextToolMode = ToolMode.None;
                toolModeSwapper._isSwitchingToolMode = false;

                InputMode inputMode = InputMode.Character | InputMode.ShipCockpit;
                if (OWInput.IsInputMode(inputMode) && toggleGunKey)
                {
                    if (toolModeSwapper._currentToolMode == SGToolmode)
                    {
                        toolModeSwapper.UnequipTool();  //If you hit the signalscope button while it's already equipped, it puts it away
                        ModHelper.Console.WriteLine("unequipped Scalestaff");
                    }
                    else
                    {
                        toolModeSwapper.EquipToolMode(SGToolmode);  //EquipToolMode's your signalscope.  Equipping stuff seems to be close to the end of things
                        ModHelper.Console.WriteLine("equipped Scalestaff");
                    }



                }

            }
        }



        //private void PutAwayOtherTools()
        //{ var currentToolMode = Locator.GetToolModeSwapper().GetToolMode();
        //   if (currentToolMode != ToolMode.Item || ToolMode.None) //if player is in a toolmode other than Item, unequips the tool
        //  {
        //     Locator.GetToolModeSwapper().UnequipTool();
        // }
        // }

        private void EyesDrillHoles()          //GameObjects have a SetActive method, the menu uses this, maybe it's single-target?  maybe I don't have to use my own thingus?
        {
            if (BigBubbon && Locator.GetPlayerCamera() != null)
            {
                Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

                Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
                var retrievedRootObject = hit.collider.transform.GetPath();
                NotificationManager.SharedInstance.PostNotification(
    new NotificationData(NotificationTarget.Player,
       $"{retrievedRootObject} Observed",   //try plopping down an object that gets the gameobject nearest its current coords?? idk
       5f, true));
            }
        }
        private void Update()
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame;         //GOAL: 
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
                UpBubbon = Keyboard.current[Up].wasPressedThisFrame;
                DownBubbon = Keyboard.current[Down].wasPressedThisFrame;//THANKS TO Raoul1808 for the tip on where to find the notification stuff!
                LeftBubbon = Keyboard.current[Left].wasPressedThisFrame;
                RightBubbon = Keyboard.current[Right].wasPressedThisFrame;
                toggleGunKey = Keyboard.current[GunToggle].wasPressedThisFrame;
            }
            if (UpBubbon)
            {
                ModHelper.Console.WriteLine("Upped your bubbon");
                EyesDrillHoles();
            }


            if (toggleGunKey)
            {
                ToggleScaleGun();
            }
        }



        void OriginalSwapperUpdateTrainwreck(ToolModeSwapper agh)
        {
            if (_pHSWAPPER._isSwitchingToolMode && !_pHSWAPPER._equippedTool.IsEquipped())
            {
                _pHSWAPPER._equippedTool = _pHSWAPPER._nextTool;
                _pHSWAPPER._nextTool = null;
                if (_pHSWAPPER._equippedTool != null)
                {
                    _pHSWAPPER._equippedTool.EquipTool();
                }
                _pHSWAPPER._currentToolMode = _pHSWAPPER._nextToolMode;
                _pHSWAPPER._nextToolMode = ToolMode.None;
                _pHSWAPPER._isSwitchingToolMode = false;
            }

            InputMode inputMode = InputMode.Character | InputMode.ShipCockpit;    //establishing the inputMode variable here saves time when lots of "if's" will be addressing either of the InputModes it's , 
            if (!_pHSWAPPER.IsNomaiTextInFocus())
            {
                _pHSWAPPER._waitForLoseNomaiTextFocus = false;  //sets _waitForLoseNomaiTextFocus to "false" as preparation for all the stuff checked this update
            }
            if (_pHSWAPPER._shipDestroyed && _pHSWAPPER._currentToolGroup == ToolGroup.Ship)    //if you're in the cockpit of a destroyed ship, ToolModeSwapper ignores you
            {
                return;
            }
            if (_pHSWAPPER._currentToolMode != ToolMode.None && _pHSWAPPER._currentToolMode != ToolMode.Item && (OWInput.IsNewlyPressed(InputLibrary.cancel, inputMode | InputMode.ScopeZoom) || PlayerState.InConversation()))
            {  //I... THINK? this is what puts tools away when you hit Q or enter a conversation
                InputLibrary.cancel.ConsumeInput();                     //i think this prevents the cancel input from lingering or being cashed in elsewhere, idk
                if (_pHSWAPPER.GetAutoEquipTranslator() && _pHSWAPPER._currentToolMode == ToolMode.Translator)  //and if TranslatorAutoEquip is on, probably puts the translator away despite being focused on text?
                {
                    _pHSWAPPER._waitForLoseNomaiTextFocus = true;     //then UnequipTool will _waitForLoseNomaiTextFocus  //WHAT IS FOCUS?  DOES THE TRANSLATOR NOT UNEQUIP DURING TIMEFREEZE UNTIL YOU LOOK AWAY?
                }
                _pHSWAPPER.UnequipTool();
            }
            else if (_pHSWAPPER.IsNomaiTextInFocus() && _pHSWAPPER._currentToolMode != ToolMode.Translator && ((_pHSWAPPER.GetAutoEquipTranslator() && !_pHSWAPPER._waitForLoseNomaiTextFocus) || OWInput.IsNewlyPressed(InputLibrary.interact, inputMode)))
            {           //Equips translator when not currently equipped once you hit E
                _pHSWAPPER.EquipToolMode(ToolMode.Translator);
                if (_pHSWAPPER._firstPersonManipulator.GetFocusedNomaiText() != null && _pHSWAPPER._firstPersonManipulator.GetFocusedNomaiText().CheckTurnOffFlashlight())
                {  //and if there's FocusedNomaiText (whatever that is) and the text says to turn off your flashlight, then it turns off your flashlight (don't remember encountering this...)
                    Locator.GetFlashlight().TurnOff(false);  //sets TurnOff to false.  That's a double-double negative!
                }
            }
            else if (_pHSWAPPER._currentToolMode == ToolMode.Translator && !_pHSWAPPER.IsNomaiTextInFocus() && _pHSWAPPER.GetAutoEquipTranslator())
            { //unequips translator if no spirals are in focus and AutoEquipTranslator is active. (when it's off, the translator stays equipped until you manually stow it.  coolio)
                _pHSWAPPER.UnequipTool();
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.probeLaunch, inputMode)) //PRESSED PROBELAUNCH
            {
                if (_pHSWAPPER._currentToolGroup == ToolGroup.Suit && _pHSWAPPER._itemCarryTool.GetHeldItemType() == ItemType.DreamLantern)
                {
                    return; //scout doesn't launch or deploy if you're wearing a suit while holding an artifact.
                }
                if (((_pHSWAPPER._currentToolMode == ToolMode.None || _pHSWAPPER._currentToolMode == ToolMode.Item) && Locator.GetPlayerSuit().IsWearingSuit(false)) || ((_pHSWAPPER._currentToolMode == ToolMode.None || _pHSWAPPER._currentToolMode == ToolMode.SignalScope) && OWInput.IsInputMode(InputMode.ShipCockpit)))
                { //( You're suitless, armed with nothing, naught to your name, but perhaps an item.) OR (you have naught, or maybe your signalscope.)  Regardless, in your cockpit, you right click, or otherwise deploy your scout,
                    _pHSWAPPER.EquipToolMode(ToolMode.Probe);  //and it works idk what this does actually
                }
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.signalscope, inputMode | InputMode.ScopeZoom))
            {
                if (PlayerState.InDreamWorld())
                {
                    return;  //can't deploy signalscope in the dreamworld.  idk why ScopeZoom is accounted for here.
                }
                if (_pHSWAPPER._currentToolMode == ToolMode.SignalScope)
                {
                    _pHSWAPPER.UnequipTool();  //If you hit the signalscope button while it's already equipped, it puts it away
                }
                else
                {
                    _pHSWAPPER.EquipToolMode(ToolMode.SignalScope);  //EquipToolMode's your signalscope.  Equipping stuff seems to be close to the end of things
                }
            }
            bool flag = _pHSWAPPER._itemCarryTool.UpdateInteract(_pHSWAPPER._firstPersonManipulator, _pHSWAPPER.IsItemToolBlocked());  //UpdateInteract handles both picking up and setting down i think? 
            if (!_pHSWAPPER._itemCarryTool.IsEquipped() && flag)  //if a carried item tool isn't equipped and can't be placed(?)
            {
                _pHSWAPPER.EquipToolMode(ToolMode.Item);  //equips the itemTool?
                return;
            }
            if (_pHSWAPPER._itemCarryTool.GetHeldItem() != null && _pHSWAPPER._currentToolMode == ToolMode.None && OWInput.IsInputMode(InputMode.Character) && !OWInput.IsChangePending())
            {//if you're holding a not-currently-equipped item, and current toolmode is none, and you're not at a cockpit, and no change is pending
                _pHSWAPPER.EquipToolMode(ToolMode.Item);  //equip the itemTool 
            }
        }

        private void FixedUpdate()  //ripped from BlackHolePortalGun
        { }


        //this could turn into a whole hellish thing but i'm visualising it.  toggle between scout and another right-click-controlled thingus

        private static bool PleaseMaamMayIPrivateMessageYou(Collider bumpedCollider)
        {
            if (bumpedCollider.attachedRigidbody != null)  //The crime about to be committed was first performed by LocoChoco in Slate's Shipyard.  I desecrate in the craters of giants
            {
                var bodyThatMightEnter = bumpedCollider.attachedRigidbody.GetComponent<AstroObject>();
                if (bodyThatMightEnter == null)
                {
                    bumpedCollider.attachedRigidbody.GetComponentInChildren<AstroObject>();
                };
                if (bodyThatMightEnter != null)
                {
                    Instance.ModHelper.Console.WriteLine($"Prevented {bumpedCollider?.GetAttachedOWRigidbody()?.ToString()} from vanishing");
                    return false;
                };
            }
            return true;
        }

        public class ScaleGun420PatchClass
        {

            //in ToolModeSwapper's Awake method, it gets each individual tool by name/type and stores them in its class fields.  This is a problem.  I can't add fields to existing things.

            [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.Update))]
            public static bool ToolModeSwapper_Update_Prefix(PlayerTool toolMode)
            { return true; }  //Owl said i might not even have to patch ToolmodeSwapper.Update?  idk how not but

            [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
            public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode, ToolModeSwapper __instance, PlayerTool playerTool)  //instance is for referencing the class currently performing the method you're patching 
            {
                ToolMode scalegunMode = Instance.SGToolmode;
                if (mode != scalegunMode) { return true; }

                {
                    mode = scalegunMode;
                    playerTool = ScaleGun420.Instance._theGunTool;
                }

                //the essentials
                if (__instance._equippedTool != playerTool)  //if the ToolModeSwapper's currently-equipped tool isn't the newly-set playerTool,
                {
                    if (__instance._equippedTool != null)    //and isn't null
                    {
                        __instance._equippedTool.UnequipTool();   //unequip the equipped tool,
                        __instance._nextToolMode = mode;     //set the Instance.SGToolmode mode as ToolModeSwapper's _nextToolMode,
                        __instance._nextTool = playerTool;        //
                        __instance._isSwitchingToolMode = true;
                        return false;                                         //if it's in a prefix that returns Bool, you have to have "return false" not just "return"
                    }
                    playerTool.EquipTool();
                    __instance._equippedTool = playerTool;
                    __instance._currentToolMode = mode;
                    __instance._nextToolMode = ToolMode.None;
                }
                return false;
            }

        }
    }
}




