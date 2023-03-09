using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScaleGun420
{
    public static class Extensions
    {
        public static string GetPath(this Transform current)    //Xen says New Horizons uses this.  It has to be in a static class, so that's why it's in Extensions
        {
            if (current.parent == null) return current.name;
            return current.parent.GetPath() + "/" + current.name; //literally just digs until it hits bedrock.  Let player manually scroll back up somehow
        }
    }
}
