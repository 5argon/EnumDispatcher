namespace E7.EnumDispatcher
{
    /// <summary>
    /// It make checking for a flag's existence of any action works in a job.
    /// </summary>
    public struct ActionFlag
    {
        internal int flagValue;
        public ActionFlag(string stringFlag, EnumTypeManager etm) => this.flagValue = etm.StringFlagToInt(stringFlag);
        public override string ToString() => $"Flag inner value : {flagValue.ToString()}";
    }
}