using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScaleGun420
{
    public static class TheLogGoober
    {
        public static void WriteLine(string msg) => ScaleGun420Modbehavior.Instance.ModHelper.Console.WriteLine(msg);
    }
}
