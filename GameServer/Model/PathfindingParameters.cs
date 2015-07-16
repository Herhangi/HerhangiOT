namespace HerhangiOT.GameServer.Model
{
    public class PathfindingParameters
    {
        public bool FullPathSearch { get; set; }
        public bool ClearSight { get; set; }
        public bool AllowDiagonal { get; set; }
        public bool KeepDistance { get; set; }
        public int MaxSearchDist { get; set; }
        public int MinTargetDist { get; set; }
        public int MaxTargetDist { get; set; }

        public PathfindingParameters()
        {
		    FullPathSearch = true;
		    ClearSight = true;
		    AllowDiagonal = true;
		    KeepDistance = false;
		    MaxSearchDist = 0;
		    MinTargetDist = -1;
		    MaxTargetDist = -1;
	    }
    }
}
