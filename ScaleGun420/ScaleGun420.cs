using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tessellation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

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
        public ScalegunTool _theGunTool;
        public Key GunToggle;        //Idk if it'll be more or less work to prevent gun from working while in ship.  guess we'll find out
        public bool toggleGunKey; //whether right-click & other scout-related actions reach the Scalegun instead
        public bool _gunIsEquipped;

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

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
                ModHelper.Events.Unity.FireOnNextUpdate(
    () =>
    {
        this._theGunTool = this.GetRequiredComponentInChildren<ScalegunTool>();        //Same as how the NomaiTranslator tool's Awake() method declares NomaiTranslatorProp
        _theGunTool.SpawnNomaiStaff();
    }
);

            };
            //Local position: 0.5496 -1.121 -0.119
            //Rotation 343.8753 200.2473 345.2718

            // GlobalMessenger<ProbeLauncher>.AddListener("ProbeLauncherEquipped", new Callback<ProbeLauncher>(this.OnProbeLauncherEquipped)); //Listens for ProbeLauncher events
            // GlobalMessenger<ProbeLauncher>.AddListener("ProbeLauncherUnequipped", new Callback<ProbeLauncher>(this.OnProbeLauncherUnequipped));
        }
        //private void booty()

        //  instancedStaff.AddComponent<ScalegunTool>();
        //   _theGunTool = instancedStaff.GetComponent<ScalegunTool>();    //DO I STILL NEED TO DO THIS NOW THE STAFF IS ALREADY INSIDE ScalegunTool'S CLASS?
        // instancedStaff.SetActive(true);


        public override void Configure(IModConfig config)
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

            if (_gunIsEquipped)
            {
                _gunIsEquipped = false;
                _theGunTool.UnequipTool();
                ModHelper.Console.WriteLine("unequipped Scalestaff");
            }
            else if (!_gunIsEquipped)
            {
                //PutAwayOtherTools();
                _gunIsEquipped = true;
                _theGunTool.EquipTool();
                ModHelper.Console.WriteLine("equipped Scalestaff");

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

        // public class ScaleGun420PatchClass
        // {
        //   [HarmonyPrefix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.AllowInput))]
        //  }
    }
}


