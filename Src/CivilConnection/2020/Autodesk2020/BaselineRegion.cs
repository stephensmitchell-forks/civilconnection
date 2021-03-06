﻿// Copyright (c) 2016 Autodesk, Inc. All rights reserved.
// Author: paolo.serra@autodesk.com
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime;
using System.Runtime.InteropServices;

using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AECC.Interop.UiRoadway;
using Autodesk.AECC.Interop.Roadway;
using System.Reflection;

using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Geometry;

namespace CivilConnection
{
    /// <summary>
    /// BaselineRegion object type.
    /// </summary>
    public class BaselineRegion
    {
        #region PRIVATE PROPERTIES

        private Baseline _baseline;
        private AeccBaselineRegion _blr;
        private double _start;
        private double _end;
        private double[] _stations;
        private IList<Featureline> _featurelines = new List<Featureline>();
        private IList<Subassembly> _subassemblies = new List<Subassembly>();
        // private IList<AppliedAssembly> _appliedAssemblies = new List<AppliedAssembly>();
        private int _index;
        private IList<IList<IList<AppliedSubassemblyShape>>> _shapes = new List<IList<IList<AppliedSubassemblyShape>>>();
        private IList<IList<IList<AppliedSubassemblyLink>>> _links = new List<IList<IList<AppliedSubassemblyLink>>>();
        private string _assembly;

        /// <summary>
        /// Gets the internal element.
        /// </summary>
        /// <value>
        /// The internal element.
        /// </value>
        internal object InternalElement { get { return this._blr; } }

        #endregion

        #region PUBLIC PROPERTIES

        /// <summary>
        /// Gets the region start station.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public double Start { get { return _start; } }
        /// <summary>
        /// Gets theregion end station.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public double End { get { return _end; } }
        /// <summary>
        /// Gets the region stations.
        /// </summary>
        /// <value>
        /// The stations.
        /// </value>
        public double[] Stations { get { return _stations; } }
        /// <summary>
        /// Gets the region subassemblies.
        /// </summary>
        /// <value>
        /// The subassemblies.
        /// </value>
        public IList<Subassembly> Subassemblies 
        { 
            get 
            {
                if (this._subassemblies.Count != 0)
                {
                    return this._subassemblies;
                }

                // Calculate these objects only when they are required
                foreach (AeccAppliedSubassembly asa in this._blr.AppliedAssemblies.Item(0).AppliedSubassemblies)
                {
                    try
                    {
                        // this._appliedAssemblies.Add(new AppliedAssembly(this, a, a.Corridor));  // TODO: verify why this is a list instead of a single applied assembly...

                        try
                        {
                            this._subassemblies.Add(new Subassembly(asa.SubassemblyDbEntity, asa.Corridor));
                        }
                        catch (Exception ex)
                        {
                            this._subassemblies.Add(null);

                            Utils.Log(string.Format("ERROR: {0}", ex.Message));

                            throw new Exception("Subassemblies Failed\n\n" + ex.Message);
                        }
                        
                        // break;
                    }
                    catch (Exception ex)
                    {
                        Utils.Log(string.Format("ERROR: {0}", ex.Message));

                        throw new Exception("Applied Assemblies Failed\n\n" + ex.Message);
                    }
                }

                return this._subassemblies;
            } 
        }

        /// <summary>
        /// Gets the relative starting station for the BaselineRegion.
        /// </summary>
        public double RelativeStart { get { return 0; } }

        /// <summary>
        /// Gets the relative ending station for the BaselineRegion.
        /// </summary>
        public double RelativeEnd { get { return _end - _start; } }

        /// <summary>
        /// Gets the normalized starting station for the BaselineRegion.
        /// </summary>
        public double NormalizedStart { get { return 0; } }

        /// <summary>
        /// Gets the normalized starting station for the BaselineRegion.
        /// </summary>
        public double NormalizedEnd { get { return 1; } }

        /// <summary>
        /// Gets the Baselineregion index value.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// Gets the Shapes profile of the applied subassemblies in the BaselineRegion.
        /// </summary>
        public IList<IList<IList<AppliedSubassemblyShape>>> Shapes 
        { 
            get 
            {
                if (this._shapes.Count != 0)
                {
                    return this._shapes;
                }

                Utils.Log(string.Format("BaselineRegion.Shapes started...", ""));

                double[] stations = this._blr.AppliedAssemblies.Stations;

                int stationCounter = 0;

                // Get the Applied Subassembly Shapes
                foreach (AeccAppliedAssembly a in this._blr.AppliedAssemblies)
                {
                    double station = Math.Round(stations[stationCounter], 5);

                    Utils.Log(string.Format("AppliedAssembly Station {0} started...", station));

                    IList<IList<AppliedSubassemblyShape>> a_list = new List<IList<AppliedSubassemblyShape>>();

                    // int subCounter = 0;

                    var coll = a.AppliedSubassemblies.Cast<AeccAppliedSubassembly>().GroupBy(x => x.SubassemblyDbEntity.Handle);

                    foreach (var group in coll)
                    {
                        Utils.Log(string.Format("AssemblyGroup started...", ""));

                        foreach (AeccAppliedSubassembly s in group)
                        {                            
                            string handle = s.SubassemblyDbEntity.Handle;

                            IList<AppliedSubassemblyShape> s_list = new List<AppliedSubassemblyShape>();

                            string subname = s.SubassemblyDbEntity.DisplayName;

                            Utils.Log(string.Format("AppliedSubassembly {0} started...", subname));

                            int counter = 0;

                            foreach (AeccCalculatedShape cs in s.CalculatedShapes)
                            {
                                Utils.Log(string.Format("CalculatedShape started...", ""));

                                var codes = cs.CorridorCodes.Cast<string>().ToList();

                                string name = string.Join("_", this._baseline.CorridorName, this._baseline.Index, this.Index, this._assembly, subname, handle, counter);  // verify the names

                                IList<Curve> curves = new List<Curve>();

                                IList<Point> pts = new List<Point>();  // 20190413

                                foreach (AeccCalculatedLink cl in cs.CalculatedLinks)
                                {
                                    //Utils.Log(string.Format("CalculatedLink started...", ""));

                                    // IList<Point> pts = new List<Point>();

                                    foreach (AeccCalculatedPoint cp in cl.CalculatedPoints)
                                    {
                                        //Utils.Log(string.Format("CalculatedPoint started...", ""));

                                        var soe = cp.GetStationOffsetElevationToBaseline();

                                        var pt = this._baseline._baseline.StationOffsetElevationToXYZ(soe);

                                        Point p = Point.ByCoordinates(pt[0], pt[1], pt[2]);

                                        if (!pts.Contains(p))
                                        {
                                            pts.Add(p); 
                                        }

                                        //Utils.Log(string.Format("CalculatedPoint completed.", ""));
                                    }

                                    //Utils.Log(string.Format("CalculatedLink completed.", ""));
                                }

                                PolyCurve pro = null;

                                try
                                {
                                    pro = PolyCurve.ByPoints(pts, true);
                                }
                                catch (Exception ex)
                                {
                                    Utils.Log(string.Format("ERROR: Cannot Create PolyCurve By Points {0}", ex.Message));
                                }

                                if (pro != null)
                                {
                                    AppliedSubassemblyShape sh = new AppliedSubassemblyShape(name, pro, codes, station);

                                    s_list.Add(sh);

                                    ++counter; 
                                }

                                Utils.Log(string.Format("CalculatedShape completed.", ""));
                            }

                            a_list.Add(s_list);

                            // ++subCounter;

                            Utils.Log(string.Format("AppliedSubassembly completed.", ""));
                        }

                        Utils.Log(string.Format("AppliedAssembly completed.", ""));
                    }

                    this._shapes.Add(a_list);

                    ++stationCounter;

                    Utils.Log(string.Format("AssemblyGroup completed.", ""));
                }

                //return _shapes.Select(a => a.Select(s => s.Select(sh => sh.Geometry).ToList()).ToList()).ToList();

                Utils.Log(string.Format("BaselineRegion.Shapes completed.", ""));

                return _shapes;
            }
        }

        /// <summary>
        /// Gets the Links profile of the applied subassemblies in the BaselineRegion.
        /// </summary>
        //public List<List<List<Geometry>>> Links
        public IList<IList<IList<AppliedSubassemblyLink>>> Links
        {
            get
            {
                if (this._links.Count != 0)
                {
                    return this._links;
                }

                Utils.Log(string.Format("BaselineRegion.Links started...", ""));

                double[] stations = this._blr.AppliedAssemblies.Stations;

                int stationCounter = 0;

                // Get the Applied Subassembly Links
                foreach (AeccAppliedAssembly a in this._blr.AppliedAssemblies)
                {
                    double station = Math.Round(stations[stationCounter], 5);

                    IList<IList<AppliedSubassemblyLink>> a_list = new List<IList<AppliedSubassemblyLink>>();

                    // int subCounter = 0;

                    var coll = a.AppliedSubassemblies.Cast<AeccAppliedSubassembly>().GroupBy(x => x.SubassemblyDbEntity.Handle);

                    foreach (var group in coll)
                    {
                        foreach (AeccAppliedSubassembly s in group)
                        {
                            string handle = s.SubassemblyDbEntity.Handle;

                            IList<AppliedSubassemblyLink> s_list = new List<AppliedSubassemblyLink>();

                            string subname = s.SubassemblyDbEntity.DisplayName;

                            int counter = 0;

                            foreach (AeccCalculatedLink cl in s.CalculatedLinks)
                            {
                                var codes = cl.CorridorCodes.Cast<string>().ToList();

                                string name = string.Join("_", this._baseline.CorridorName, this._baseline.Index, this.Index, this._assembly, subname, handle, counter);  // verify the names

                                IList<Point> pts = new List<Point>();

                                foreach (AeccCalculatedPoint cp in cl.CalculatedPoints)
                                {
                                    var pt = this._baseline._baseline.StationOffsetElevationToXYZ(cp.GetStationOffsetElevationToBaseline());

                                    Point p = Point.ByCoordinates(pt[0], pt[1], pt[2]);

                                    pts.Add(p);
                                }

                                pts = Point.PruneDuplicates(pts, 0.00001).ToList();

                                if (pts.Count > 0)
                                {
                                    PolyCurve poly = null;

                                    try
                                    {
                                        poly = PolyCurve.ByPoints(pts);
                                    }
                                    catch (Exception ex)
                                    {
                                       Utils.Log(string.Format("ERROR: Cannot Create PolyCurve By Points {0}", ex.Message));
                                    }

                                    if (poly != null)
                                    {
                                        AppliedSubassemblyLink sh = new AppliedSubassemblyLink(name, poly, codes, station);

                                        s_list.Add(sh);

                                        ++counter;  
                                    }
                                }
                            }

                            a_list.Add(s_list);

                            // ++subCounter;
                        } 
                    }

                    this._links.Add(a_list);

                    ++stationCounter;
                }

                //return _links.Select(a => a.Select(s => s.Select(l => l.Geometry).ToList()).ToList()).ToList();

                Utils.Log(string.Format("BaselineRegion.Links completed.", ""));

                return _links;
            }
        }

        #endregion

        #region CONSTRUCTOR

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="baseline">The baseline that holds the baseline region.</param>
        /// <param name="blr">The internal AeccBaselineRegion</param>
        /// <param name="i">The baseline region index</param>
        internal BaselineRegion(Baseline baseline, AeccBaselineRegion blr, int i)
        {
            this._baseline = baseline;

            this._blr = blr;

            this._index = i;

            this._assembly = blr.AssemblyDbEntity.DisplayName;

            try 
            {
                this._start = blr.StartStation; //  Math.Round(blr.StartStation, 5);  // TODO get rid of the roundings
            }
            catch (Exception ex)
            {
                Utils.Log(string.Format("ERROR: Start Station Failed\t{0}", ex.Message));

                throw new Exception("Start Station Failed\n\n" + ex.Message);
            }

            try
            {

                this._end = blr.EndStation; //  Math.Round(blr.EndStation, 5);
            }
            catch (Exception ex)
            {
                Utils.Log(string.Format("ERROR: End Station Failed\t{0}", ex.Message));

                throw new Exception("End Station Failed\n\n" + ex.Message);
            }

            try
            {
                this._stations = blr.GetSortedStations();
            }
            catch (Exception ex)
            {
                Utils.Log(string.Format("ERROR: Sorted Stations Failed\t{0}", ex.Message));

                throw new Exception("Sorted Stations Failed\n\n" + ex.Message);
            }

            #region OLDCODE
            //foreach (AeccAppliedAssembly a in blr.AppliedAssemblies)
            //{
            //    try
            //    {
            //        this._appliedAssemblies.Add(new AppliedAssembly(this, a, a.Corridor));  // TODO: verify why this is a list instead of a single applied assembly...
            //        // break;
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new Exception("Applied Assemblies Failed\n\n" + ex.Message);
            //    }
            //}

            //foreach (AppliedAssembly aa in this._appliedAssemblies)
            //{
            //    foreach (AeccAppliedSubassembly asa in aa._appliedSubassemblies)
            //    {
            //        try
            //        {
            //            this.Subassemblies.Add(new Subassembly(asa.SubassemblyDbEntity, asa.Corridor));
            //        }
            //        catch (Exception ex)
            //        {
            //            this.Subassemblies.Add(null);
            //            throw new Exception("Applied Subassemblies Failed\n\n" + ex.Message);
            //        }
            //    }
            //}
            #endregion
        }

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Public textual representation of the Dynamo node preview
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("BaselineRegion(Start = {0}, End = {1})", Math.Round(this.Start, 2).ToString(), Math.Round(this.End, 2).ToString());
        }

        #endregion
    }
}
