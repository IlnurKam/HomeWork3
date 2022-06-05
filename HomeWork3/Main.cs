using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace HomeWork3
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            RoofType type = doc.GetElement(id) as RoofType;
            if (type == null)
            {
                TaskDialog.Show("Ошибка", "Это не тип крыши");
                return Result.Failed;
            }
            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 20, 20)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 20, 20), new XYZ(0, 40, 0)));
            Level level = doc.ActiveView.GenLevel;
            if (level == null)
            {
                TaskDialog.Show("Ошибка", "Неправильный вид");
                return Result.Failed;
            }
            using (Transaction tr = new Transaction(doc))
            {
                tr.Start("Создать контур урыши");
                ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
                doc.Create.NewExtrusionRoof(curveArray, plane, level, type, 0, 40);
                tr.Commit();
            }

            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1=listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            Level level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            return Result.Succeeded;
        //}
        //public void CreateWalls(Document doc, Level level1, Wall wall)
        //{
            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall1 = Wall.Create(doc, line, level1.Id, false);
                Wall wall = wall1;
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            AddDoors(doc, level1, walls[0]);
            AddWindows(doc, level1, walls[1]);
            //AddRoof(doc, level2, walls, walls);
            transaction.Commit();
        }

        

        private void AddDoors(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) * 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        private void AddWindows(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowsType=new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 0610 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve= wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1-point2)/2;

            if (!windowsType.IsActive)
                windowsType.Activate();

            doc.Create.NewFamilyInstance(point, windowsType, wall, level1, StructuralType.NonStructural);
        }
    }
}

//private void AddRoof(Document doc, object level2, object walls, object walls)
//{
//    RoofType roofType = new FilteredElementCollector(doc)
//        .OfClass(typeof(RoofType))
//        .OfType<RoofType>()
//        .Where(x => x.Name.Equals("Типовой - 125 мм"))
//        .Where(x => x.FamilyName.Equals("Базовая крыша"))
//        .FirstOrDefault();

//    double wallWidth = walls[0].Width;
//    double dt = wallWidth / 2;
//    List<XYZ> points = new List<XYZ>();
//    points.Add(new XYZ(-dt, -dt, 0));
//    points.Add(new XYZ(dt, -dt, 0));
//    points.Add(new XYZ(dt, dt, 0));
//    points.Add(new XYZ(-dt, dt, 0));
//    points.Add(new XYZ(-dt, -dt, 0));

//    Application application = doc.Application;
//    CurveArray footprint = application.Create.NewCurveArray();
//    for (int i = 0; i < 4; i++)
//    {
//        LocationCurve curve = walls[i].Location as LocationCurve;
//        XYZ p1 = curve.Curve.GetEndPoint(0);
//        XYZ p2 = curve.Curve.GetEndPoint(1);
//        Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
//        footprint.Append(line);
//    }
//    ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
//    FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, (Level)level2, roofType, out footPrintToModelCurveMapping);
//    //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
//    //iterator.Reset();
//    //while (iterator.MoveNext())
//    //{
//    //    ModelCurve modelCurve = iterator.Current as ModelCurve;
//    //    footprintRoof.set_DefinesSlope(modelCurve, true);
//    //    footprintRoof.set_SlopeAngle(modelCurve, 0.5);

//    //}
//    foreach (ModelCurve m in footPrintToModelCurveMapping)
//    {
//        footprintRoof.set_DefinesSlope(m, true);
//        footprintRoof.set_SlopeAngle(m, 0.5);
//    }
//}


