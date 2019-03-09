using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Fighter
    {
        public string fighterName;
        public Client user;
        int health;
        private static int MAXHEALTH = Server.MaxHealthSet;
        Random rand = new Random();
        public enum Ability
        {
            Strength,
            Dodge
        }
        public Ability StrongAbility;
        public int Health
        {
            get => health;
            set
            {
                if (value > MAXHEALTH)
                {
                    health = MAXHEALTH;
                }
                else
                {
                    health = value;
                }
            }
        }
        public Fighter(string name, Client client)
        {
            fighterName = name;
            user = client;
        }
        public void InitializeFighter()
        {
            StrongAbility = rand.Next(101) < 50 ? Ability.Strength : Ability.Dodge;
            health = rand.Next(MAXHEALTH - 60, MAXHEALTH - 20);
        }
        public static void InitializeSettings()
        {
            MAXHEALTH = Server.MaxHealthSet;
        }
    }
}
