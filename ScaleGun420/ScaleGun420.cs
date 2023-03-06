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

        public GameObject _lookingAt;
        public GameObject _recentTargetObject;

        public static ScaleGun420 Instance;
        public void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }

        private void Start()
        {

            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(ScaleGun420)} is loaded!", MessageType.Success);
            // INewHorizons NewHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");


            // Example of accessing game code.
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
            };
        }
        public override void Configure(IModConfig config)
        {
            Big = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Big Your Ball"));
            Small = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Small Your Ball"));

            //Up = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Up a layer (default: \"UpArrow\")"));
            //Down = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Down a layer (default: \"DownArrow\")"));
            // Left = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Left in layer (default: \"LeftArrow\")"));
            //  Right = (Key)System.Enum.Parse(typeof(Key), config.GetSettingsValue<string>("Right in layer (default: \"RightArrow\")"));
        }

        private void Update()
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame;         //GOAL: 
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
                                                                             //UpBubbon = Keyboard.current[Up].wasPressedThisFrame;
                                                                             // DownBubbon = Keyboard.current[Down].wasPressedThisFrame;//THANKS TO Raoul1808 for the tip on where to find the notification stuff!
                                                                             // LeftBubbon = Keyboard.current[Left].wasPressedThisFrame;
                                                                             // RightBubbon = Keyboard.current[Right].wasPressedThisFrame;
            }
            if (BigBubbon)
            {
                EyesDrillHoles();
            }
        }
        private void EyesDrillHoles()
        {
            if (BigBubbon)
            {
                Vector3 fwd = Locator.GetPlayerCamera().transform.forward;  //fwd is a Vector-3 that transforms forward relative to the playercamera

                Physics.Raycast(Locator.GetPlayerCamera().transform.position, fwd, out RaycastHit hit, 50000, OWLayerMask.physicalMask);
                var retrievedRootObject = hit.collider.transform.GetPath();
                NotificationManager.SharedInstance.PostNotification(
   new NotificationData(
NotificationTarget.Player,
       $"{retrievedRootObject} Observed",   //try plopping down an object that gets the gameobject nearest its current coords?? idk
       5f,
       true));
            }
        }

        private void FixedUpdate()  //ripped from BlackHolePortalGun
        { }
    }
}
        //this could turn into a whole hellish thing but i'm visualising it.  toggle between scout and another right-click-controlled thingus

        //private static bool PleaseMaamMayIPrivateMessageYou(Collider bumpedCollider)
       // {{{
                    //if (bumpedCollider.attachedRigidbody != null)  //The crime about to be committed was first performed by LocoChoco in Slate's Shipyard.  I desecrate in the craters of giants
//var bodyThatMightEnter = bumpedCollider.attachedRigidbody.GetComponent<AstroObject>() ?? bumpedCollider.attachedRigidbody.GetComponentInChildren<AstroObject>(); //
                    //{Instance.ModHelper.Console.WriteLine($"Prevented {bumpedCollider?.GetAttachedOWRigidbody()?.ToString()} from vanishing"); return false; }}}}}} //return true;

