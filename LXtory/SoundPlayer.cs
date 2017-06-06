using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LXtory
{
    class SoundPlayer
    {
        private static System.Media.SoundPlayer player;
        public static void Init(string path)
        {
            try
            {
                player = new System.Media.SoundPlayer(path);
            }
            catch (Exception)
            {
                player = new System.Media.SoundPlayer(Properties.Resources.pop);
            }
        }
        public static void PlaySound()
        {
            try
            {
                player.Play();
            }
            catch (Exception)
            {
            }
        }
    }
}
