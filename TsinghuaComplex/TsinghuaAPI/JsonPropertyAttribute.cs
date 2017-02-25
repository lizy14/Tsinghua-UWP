using System;

namespace TsinghuaComplex
{
    internal class DataMember : Attribute
    {
        private string v;

        public DataMember(string v)
        {
            this.v = v;
        }
    }
}