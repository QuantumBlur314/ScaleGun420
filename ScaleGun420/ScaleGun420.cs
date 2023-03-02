using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScaleGun420
{
    public class ScaleGun420 : ModBehaviour
    {

        public static List<OWRigidbody> _bR95growQueue = new(8);//establishes my own _growQueue (with blackjack, and hookers)
        public OWRigidbody _bR95growingBody;
        public float _bR95nextGrowCheckTime;
        public Key Big;                                                 //Grows all OWRigidbodies on _VanishBlacklist to normal size
        public bool BigBubbon;
        public Key Small;
        public bool SmallBubbon;

        public static ScaleGun420 Instance;
        private void Awake()
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
        }
        private void Update()
        {
            if (!OWInput.IsInputMode(InputMode.Menu))                //if the player isn't in the menu (RECOMMEND THIS TO BLOCKS MOD PERSON)
            {
                BigBubbon = Keyboard.current[Big].wasPressedThisFrame; ;         //GOAL: 
                SmallBubbon = Keyboard.current[Small].wasPressedThisFrame;   //BHPG listened for .wasReleasedThisFrame here; if this doesn't work, just do that
            }
        }
        private void FixedUpdate()  //stolen from WhiteHoleVolume.FixedUpdate then mangled beyond recognition
        {
            if (_bR95growingBody != null)
            {
                _bR95growingBody.SetLocalScale(_bR95growingBody.GetLocalScale() * 1.05f);
                if (_bR95growingBody.GetLocalScale().x >= 1f)

                    BR95FinishGrowing(_bR95growingBody);
                _bR95growingBody = null;
                return;
            }

            else if (_bR95growQueue.Count > 0)
            {
                if (_bR95growQueue[0] == null)
                {
                    _bR95growQueue.RemoveAt(0);
                    return;
                }
                if (Time.time > _bR95nextGrowCheckTime)
                {
                    _bR95nextGrowCheckTime = Time.time + 1f;
                    _bR95growingBody = _bR95growQueue[0];
                    _bR95growQueue.RemoveAt(0);
                }
            }
        }

        private void BR95FinishGrowing(OWRigidbody body)  //When BR95FinishGrowing() is called, it will expect whatever's in its parentheses to be OWRigidbody, and will treat them as "body"
        {
            body.SetLocalScale(Vector3.one);
        }

    }
}