using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public static class LogGoob
    {
        public static void WriteLine(string msg, MessageType grob = MessageType.Info) => ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine(msg, grob);


        //thanks again to Raoul1808 for notification help
        public static void Scream(string notification, NotificationTarget target = NotificationTarget.All )
        {
            NotificationManager.SharedInstance.PostNotification(
                new NotificationData( target, notification, 5f, false));

            Locator.GetPlayerAudioController().PlaySuitWarning();
        }

    }
}
