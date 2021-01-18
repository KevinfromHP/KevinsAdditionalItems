using System;
using System.Collections.Generic;
using System.Text;
using TILER2;
using RoR2;

namespace KevinfromHP.KevinsAdditions
{
    // VirtItem_V2 exists so I can add more methods to Item_V2
    public abstract class VirtItem_V2<T> : VirtItem_V2 where T : VirtItem_V2<T>
    {
        public static T instance { get; private set; }

        public VirtItem_V2()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class VirtItem_V2 : Item_V2
    {
        public virtual void StoreItemCount(CharacterBody body)
        {
        }
    }
}
