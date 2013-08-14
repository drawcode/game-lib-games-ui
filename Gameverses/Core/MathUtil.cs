using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Gameverses {

    public class MathUtil {

        public static int GenerateRandomNumberWithRange(int min, int max) {
            System.Random random = new System.Random();
            return random.Next(min, max);
        }
    }
}