# -*- coding: utf-8 -*-
import clr
import math
import re
from collections import defaultdict

clr.AddReference("RevitServices")
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

clr.AddReference("RevitAPI")
clr.AddReference("RevitAPIUI")
import Autodesk.Revit.DB as _revitdb
from Autodesk.Revit.DB import *
# from Autodesk.Revit.UI import TaskDialog
# -*- coding: utf-8 -*-
import clr
import math
import re
import traceback
from collections import defaultdict

clr.AddReference('RevitServices')
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
import Autodesk.Revit.DB as _revitdb
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI import TaskDialog

clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import *

clr.AddReference('System.Windows.Forms')
clr.AddReference('System.Drawing')
from System.Windows.Forms import Form, ComboBox, Label, Button, FormStartPosition, ComboBoxStyle
from System.Drawing import Point as WinPoint, Size

doc = DocumentManager.Instance.CurrentDBDocument
uiapp = DocumentManager.Instance.CurrentUIApplication
app = uiapp.Application

# Globals
red_lines = []
blue_lines = []

# JSON rules
CADENA_JSON = {
    "Bayern": {
        "GE-GI": {
            "Dachneigung <=70": {
                "Traufseite": ['((WH1+1/3*DH)*0.2)', 3],
                "Giebelseite": [['((WH1+DH)*0.2)', '(WH1*0.2)'], 3],
                "Dachaufbauten_auf_der_Traufseite": ['(WH1*0.2)', 3],
                "Dachaufbauten_auf_der_Giebelseite": ['((WH1+1/3*DH)*0.2)', 3],
            }
        }
    }
}

# UI class
class SimpleSelectorForm(Form):
    def __init__(self, title, label_text, options):
        self.Text = title
        self.Size = Size(300, 150)
        self.StartPosition = FormStartPosition.CenterScreen
        self.FormBorderStyle = 1
        self.MaximizeBox = False

        label = Label()
        label.Text = label_text
        label.Location = WinPoint(10, 10)
        label.Size = Size(260, 20)
        self.Controls.Add(label)

        self.combo = ComboBox()
        self.combo.Location = WinPoint(10, 35)
        self.combo.Size = Size(260, 25)
        self.combo.DropDownStyle = ComboBoxStyle.DropDownList
        self.combo.Items.AddRange(options)
        if options:
            self.combo.SelectedIndex = 0
        self.Controls.Add(self.combo)

        btn = Button()
        btn.Text = 'OK'
        btn.Location = WinPoint(100, 70)
        btn.Size = Size(80, 30)
        btn.Click += self.ok_clicked
        self.Controls.Add(btn)

        self.result = None

    def ok_clicked(self, sender, event):
        self.result = self.combo.SelectedItem
        self.Close()


def get_user_selection(label_text, title, options):
    form = SimpleSelectorForm(title, label_text, options)
    form.ShowDialog()
    return form.result


def obtener_lineas_con_parametros():
    resultados = []
    elems = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements()
    for el in elems:
        geo = el.get_Geometry(Options())
        if not geo:
            continue
        p_wh = el.LookupParameter('Wandhöhe') or el.LookupParameter('Wandh\u00f6he') or el.LookupParameter('Wall Height')
        p_dh = el.LookupParameter('Deckhöhe') or el.LookupParameter('Deckh\u00f6he') or el.LookupParameter('DeckHoehe')
        p_gb = el.LookupParameter('Giebelseite')
        p_dc = el.LookupParameter('Dachaufbauten')
        if p_wh is None or p_dh is None or p_gb is None or p_dc is None:
            continue
        try:
            WH1 = p_wh.AsDouble()
        except:
            WH1 = float(p_wh.AsValueString()) if p_wh else 0.0
        try:
            DH = p_dh.AsDouble()
        except:
            DH = float(p_dh.AsValueString()) if p_dh else 0.0
        GB = p_gb
        DC = p_dc
        if WH1 > 0:
            for seg in geo:
                try:
                    if hasattr(seg, 'GetEndPoint'):
                        p1 = Point.ByCoordinates(seg.GetEndPoint(0).X, seg.GetEndPoint(0).Y, 0)
                        p2 = Point.ByCoordinates(seg.GetEndPoint(1).X, seg.GetEndPoint(1).Y, 0)
                        resultados.append([p1, p2, WH1, DH, GB, DC])
                except:
                    try:
                        from Autodesk.Revit.DB import GeometryInstance
                        if isinstance(seg, GeometryInstance):
                            inst_geo = seg.GetInstanceGeometry()
                            for g in inst_geo:
                                if hasattr(g, 'GetEndPoint'):
                                    p1 = Point.ByCoordinates(g.GetEndPoint(0).X, g.GetEndPoint(0).Y, 0)
                                    p2 = Point.ByCoordinates(g.GetEndPoint(1).X, g.GetEndPoint(1).Y, 0)
                                    resultados.append([p1, p2, WH1, DH, GB, DC])
                    except:
                        continue
    return resultados


def _convex_hull_points(point_objs):
    pts = []
    for p in point_objs:
        pts.append((float(p.X), float(p.Y)))
    pts = sorted(set(pts))
    if len(pts) < 3:
        return [Point.ByCoordinates(x, y, 0) for x, y in pts]

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower = []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    upper = []
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    hull = lower[:-1] + upper[:-1]
    return [Point.ByCoordinates(x, y, 0) for x, y in hull]


def _point_in_polygon(pt, poly_xy):
    """Ray-casting point-in-polygon test.
    pt: a point with .X and .Y (or a tuple)
    poly_xy: list of (x,y) tuples
    Returns True if inside or on edge."""
    if not poly_xy:
        return False
    try:
        x = float(pt.X)
        y = float(pt.Y)
    except Exception:
        x, y = float(pt[0]), float(pt[1])
    inside = False
    n = len(poly_xy)
    for i in range(n):
        xi, yi = poly_xy[i]
        xj, yj = poly_xy[(i + 1) % n]
        # check if point is exactly on a segment (within tolerance)
        denom = (yj - yi)
        if abs(denom) < 1e-9:
            # horizontal segment, check if y matches and x between
            if abs(y - yi) < 1e-9 and min(xi, xj) - 1e-9 <= x <= max(xi, xj) + 1e-9:
                return True
            continue
        t = (y - yi) / denom
        if 0.0 <= t <= 1.0:
            x_on_seg = xi + t * (xj - xi)
            if abs(x_on_seg - x) < 1e-9:
                return True
        # ray casting
        intersect = ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / denom + xi)
        if intersect:
            inside = not inside
    return inside


def _min_distance_to_poly(pt, poly_xy):
    """Return minimum distance from point pt=(x,y) to polygon edges defined by poly_xy list of (x,y)."""
    if not poly_xy:
        return float('inf')
    x0, y0 = pt
    min_d = float('inf')
    n = len(poly_xy)
    for i in range(n):
        xi, yi = poly_xy[i]
        xj, yj = poly_xy[(i + 1) % n]
        dx = xj - xi
        dy = yj - yi
        denom = dx * dx + dy * dy
        if denom == 0:
            # degenerative edge
            px, py = xi, yi
        else:
            t = ((x0 - xi) * dx + (y0 - yi) * dy) / denom
            t = max(0.0, min(1.0, t))
            px = xi + t * dx
            py = yi + t * dy
        d = math.hypot(x0 - px, y0 - py)
        if d < min_d:
            min_d = d
    return min_d


def obtener_lote_hull():
    """Collect lines marked as lot boundary (Losgrenze==1) and return hull points and xy tuples."""
    elems = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements()
    pts = []
    for el in elems:
        p_flag = el.LookupParameter('Losgrenze') or el.LookupParameter('LosGrenze') or el.LookupParameter('LotBoundary') or el.LookupParameter('PropertyBoundary')
        if p_flag is None:
            continue
        try:
            val = p_flag.AsInteger() if hasattr(p_flag, 'AsInteger') else int(p_flag.AsValueString())
        except:
            try:
                val = int(p_flag.AsValueString())
            except:
                val = 0
        if val != 1:
            continue
        geo = el.get_Geometry(Options())
        if not geo:
            continue
        for seg in geo:
            try:
                if hasattr(seg, 'GetEndPoint'):
                    pts.append(Point.ByCoordinates(seg.GetEndPoint(0).X, seg.GetEndPoint(0).Y, 0))
                    pts.append(Point.ByCoordinates(seg.GetEndPoint(1).X, seg.GetEndPoint(1).Y, 0))
            except:
                try:
                    from Autodesk.Revit.DB import GeometryInstance
                    if isinstance(seg, GeometryInstance):
                        for g in seg.GetInstanceGeometry():
                            if hasattr(g, 'GetEndPoint'):
                                pts.append(Point.ByCoordinates(g.GetEndPoint(0).X, g.GetEndPoint(0).Y, 0))
                                pts.append(Point.ByCoordinates(g.GetEndPoint(1).X, g.GetEndPoint(1).Y, 0))
                except:
                    continue
    if not pts:
        return [], []
    hull = _convex_hull_points(pts)
    hull_xy = [(float(p.X), float(p.Y)) for p in hull]
    return hull, hull_xy


def group_polycurves_by_connectivity(polycurves):
    if not polycurves:
        return []
    n = len(polycurves)

    def _pt_key(p):
        return (round(p.X, 6), round(p.Y, 6))

    point_to_idxs = {}
    for i, item in enumerate(polycurves):
        geom = item[0]
        pts = []
        if hasattr(geom, 'Curves'):
            for s in geom.Curves():
                pts.append(_pt_key(s.StartPoint))
                pts.append(_pt_key(s.EndPoint))
        else:
            try:
                pts.append(_pt_key(geom.StartPoint))
                pts.append(_pt_key(geom.EndPoint))
            except:
                pass
        for p in set(pts):
            point_to_idxs.setdefault(p, set()).add(i)

    visited = set()
    groups = []
    for i in range(n):
        if i in visited:
            continue
        stack = [i]
        comp = []
        while stack:
            cur = stack.pop()
            if cur in visited:
                continue
            visited.add(cur)
            comp.append(cur)
            geom = polycurves[cur][0]
            pts = []
            if hasattr(geom, 'Curves'):
                for s in geom.Curves():
                    pts.append(_pt_key(s.StartPoint))
                    pts.append(_pt_key(s.EndPoint))
            else:
                try:
                    pts.append(_pt_key(geom.StartPoint))
                    pts.append(_pt_key(geom.EndPoint))
                except:
                    pass
            for p in set(pts):
                for nb in point_to_idxs.get(p, set()):
                    if nb not in visited:
                        stack.append(nb)
        groups.append([polycurves[j][0] for j in comp])

    # Try to join each group into a PolyCurve
    joined = []
    for g in groups:
        try:
            joined.append(PolyCurve.ByJoinedCurves(g))
        except:
            try:
                pts = []
                for geom in g:
                    if hasattr(geom, 'Curves'):
                        for s in geom.Curves():
                            pts.append(s.StartPoint)
                            pts.append(s.EndPoint)
                    else:
                        try:
                            pts.append(geom.StartPoint)
                            pts.append(geom.EndPoint)
                        except:
                            pass
                joined.append(PolyCurve.ByPoints(pts, True))
            except:
                joined.append(g)
    return joined


def get_line_style_by_color(color_name):
    for gs in FilteredElementCollector(doc).OfClass(GraphicsStyle):
        cat = gs.GraphicsStyleCategory
        if cat and cat.Name.lower().startswith(color_name.lower()):
            return gs
    return None


style_red = get_line_style_by_color('Rojo_Rot')
style_blue = get_line_style_by_color('Azul_Blau')

views = FilteredElementCollector(doc).OfClass(ViewPlan)
view = None
for v in views:
    if v.ViewType == ViewType.AreaPlan and v.Name == 'Deckflächenplan':
        view = v
        break


def add_text_note(text, location):
    text_type = FilteredElementCollector(doc).OfClass(TextNoteType).FirstElement()
    return TextNote.Create(doc, view.Id, location, text, text_type.Id)


def _iter_segments(obj):
    """Yield curve segments from a PolyCurve, list of curves, or single curve.
    This flattens nested lists and supports objects with Curves()."""
    if obj is None:
        return
    if hasattr(obj, 'Curves'):
        for c in obj.Curves():
            yield c
        return
    if isinstance(obj, (list, tuple)):
        for item in obj:
            for c in _iter_segments(item):
                yield c
        return
    # fallback: single segment
    yield obj


def _split_segment_by_polygon(p1, p2, poly_xy):
    """Split segment p1->p2 by intersections with polygon edges. Coordinates as tuples (x,y)."""
    if not poly_xy:
        return [(p1, p2)]
    (x1, y1) = p1
    (x2, y2) = p2
    dx = x2 - x1
    dy = y2 - y1
    t_list = [0.0, 1.0]
    n = len(poly_xy)
    for i in range(n):
        xi, yi = poly_xy[i]
        xj, yj = poly_xy[(i + 1) % n]
        qx = xj - xi
        qy = yj - yi
        denom = dx * qy - dy * qx
        if abs(denom) < 1e-12:
            continue
        t = ((xi - x1) * qy - (yi - y1) * qx) / denom
        u = ((xi - x1) * dy - (yi - y1) * dx) / denom
        if -1e-9 <= t <= 1 + 1e-9 and -1e-9 <= u <= 1 + 1e-9:
            # clamp t to [0,1]
            t_clamped = max(0.0, min(1.0, t))
            t_list.append(t_clamped)
    t_list = sorted(set(t_list))
    segments = []
    for i in range(len(t_list) - 1):
        ta = t_list[i]
        tb = t_list[i + 1]
        ax = x1 + dx * ta
        ay = y1 + dy * ta
        bx = x1 + dx * tb
        by = y1 + dy * tb
        segments.append(((ax, ay), (bx, by)))
    return segments


def draw_detail_curves(polycurves, style, lot_xy=None, style_outside=None, debug_info=None):
    results = []
    # get Revit's short curve tolerance for skipping tiny segments
    try:
        short_tol = app.ShortCurveTolerance
    except Exception:
        # fallback to a small default in feet (Revit internal units)
        short_tol = 1e-6
    skipped = 0
    for polycurve in polycurves:
        geom = polycurve[0]
        # support both PolyCurve and Line
        if hasattr(geom, 'Curves'):
            curvas = geom.Curves()
        else:
            curvas = [geom]

        for segment in curvas:
            # support different segment types (linear curves expected)
            try:
                s_pt = segment.StartPoint if hasattr(segment, 'StartPoint') else (segment.Start if hasattr(segment, 'Start') else None)
                e_pt = segment.EndPoint if hasattr(segment, 'EndPoint') else (segment.End if hasattr(segment, 'End') else None)
                if s_pt is None or e_pt is None:
                    if hasattr(segment, 'GetEndPoint'):
                        s_pt = segment.GetEndPoint(0)
                        e_pt = segment.GetEndPoint(1)
                if s_pt is None or e_pt is None:
                    continue
            except Exception:
                continue
            # prepare 2D tuples
            p1 = (float(s_pt.X), float(s_pt.Y))
            p2 = (float(e_pt.X), float(e_pt.Y))
            # split by lot polygon if provided
            sub_segs = _split_segment_by_polygon(p1, p2, lot_xy) if lot_xy else [(p1, p2)]
            for (sa, sb) in sub_segs:
                mid = ((sa[0] + sb[0]) / 2.0, (sa[1] + sb[1]) / 2.0)
                inside_lot = _point_in_polygon(mid, lot_xy) if lot_xy else True
                use_style = style if inside_lot else (style_outside or style)
                start = _revitdb.XYZ(sa[0], sa[1], 0)
                end = _revitdb.XYZ(sb[0], sb[1], 0)
                # skip segments that are too short for Revit
                # use DistanceTo to compute length between XYZ points
                seg_len = start.DistanceTo(end)
                if seg_len <= short_tol:
                    skipped += 1
                    continue
                line = _revitdb.Line.CreateBound(start, end)
                detail = doc.Create.NewDetailCurve(view, line)
                try:
                    detail.LineStyle = use_style
                except Exception:
                    pass
                results.append(detail)
        try:
            location = _revitdb.XYZ(polycurve[1][0].X, polycurve[1][0].Y, 0)
            add_text_note(polycurve[2], location.Add(_revitdb.XYZ(0, 3, 0)))
            if polycurve[3] != "":
                add_text_note(polycurve[3], location.Add(_revitdb.XYZ(0, 2, 0)))
        except:
            pass
    # attach debug summary
    if isinstance(debug_info, dict):
        debug_info.setdefault('skipped_segments', 0)
        debug_info['skipped_segments'] += skipped
        debug_info.setdefault('short_curve_tolerance', short_tol)
    return results


def draw_grouped_curves(polycurves, style, lot_xy=None, style_outside=None, debug_info=None):
    results = []
    try:
        short_tol = app.ShortCurveTolerance
    except Exception:
        short_tol = 1e-6
    skipped = 0
    for poly in polycurves:
        # poly can be a PolyCurve, a list of curves, or a single curve
        for seg in _iter_segments(poly):
            # seg might be a Revit curve or a Dynamo curve; handle both
            try:
                s_pt = seg.StartPoint if hasattr(seg, 'StartPoint') else (seg.Start if hasattr(seg, 'Start') else None)
                e_pt = seg.EndPoint if hasattr(seg, 'EndPoint') else (seg.End if hasattr(seg, 'End') else None)
                if s_pt is None or e_pt is None:
                    if hasattr(seg, 'GetEndPoint'):
                        s_pt = seg.GetEndPoint(0)
                        e_pt = seg.GetEndPoint(1)
                if s_pt is None or e_pt is None:
                    continue
            except Exception:
                continue

            p1 = (float(s_pt.X), float(s_pt.Y))
            p2 = (float(e_pt.X), float(e_pt.Y))
            sub_segs = _split_segment_by_polygon(p1, p2, lot_xy) if lot_xy else [(p1, p2)]
            for (sa, sb) in sub_segs:
                mid = ((sa[0] + sb[0]) / 2.0, (sa[1] + sb[1]) / 2.0)
                inside_lot = _point_in_polygon(mid, lot_xy) if lot_xy else True
                use_style = style if inside_lot else (style_outside or style)
                start = _revitdb.XYZ(sa[0], sa[1], 0)
                end = _revitdb.XYZ(sb[0], sb[1], 0)
                # use DistanceTo to compute length between XYZ points
                seg_len = start.DistanceTo(end)
                if seg_len <= short_tol:
                    skipped += 1
                    continue
                line = _revitdb.Line.CreateBound(start, end)
                det = doc.Create.NewDetailCurve(view, line)
                try:
                    det.LineStyle = use_style
                except Exception:
                    pass
                results.append(det)
    if isinstance(debug_info, dict):
        debug_info.setdefault('skipped_segments', 0)
        debug_info['skipped_segments'] += skipped
        debug_info.setdefault('short_curve_tolerance', short_tol)
    return results


def main():
    # ensure debug_info and lot_xy exist even if early error occurs
    debug_info = {}
    # reset global results between runs
    global red_lines, blue_lines
    red_lines = []
    blue_lines = []
    lot_xy = []
    try:
        bundesland = get_user_selection('Seleccione Bundesland:', 'Zona', list(CADENA_JSON.keys()))
        zona = get_user_selection('Seleccione Zona:', 'Zona', list(CADENA_JSON[bundesland].keys()))
        dach = get_user_selection('Seleccione Dachneigung:', 'Techo', list(CADENA_JSON[bundesland][zona].keys()))
        user_input = CADENA_JSON[bundesland][zona][dach]

        datos = obtener_lineas_con_parametros()

        surfaces = []
        surface2 = None
        if datos:
            all_points = [d[0] for d in datos] + [d[1] for d in datos]
            hull = _convex_hull_points(all_points)
            surface2 = None
            if len(hull) >= 3:
                try:
                    poly_hull = PolyCurve.ByPoints(hull, True)
                    surface2 = Surface.ByPatch(poly_hull)
                except:
                    surface2 = None
            if surface2:
                surfaces = [surface2]
            # precompute hull xy tuples for point-in-polygon tests
            hull_xy = [(float(p.X), float(p.Y)) for p in hull] if hull else []
            # compute lot hull (property boundary) to classify AF areas against lot edges
            # prepare debug_info early
            debug_info = {}
            debug_info['hull_points'] = len(hull) if hull else 0
            debug_info['surface2_created'] = True if surface2 is not None else False
            lot_hull, lot_xy = obtener_lote_hull()
            debug_info['lot_points'] = len(lot_hull) if lot_hull else 0
        try:
            elems = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements()
            debug_info['elements'] = len(elems)
            debug_info['sample'] = []
            for e in elems[:5]:
                try:
                    wh = e.LookupParameter('Wandhöhe')
                    dh = e.LookupParameter('Deckhöhe')
                    debug_info['sample'].append({'id': e.Id.IntegerValue if hasattr(e.Id, 'IntegerValue') else str(e.Id), 'WH1': wh.AsDouble() if wh and hasattr(wh, 'AsDouble') else None, 'DH': dh.AsDouble() if dh and hasattr(dh, 'AsDouble') else None})
                except:
                    debug_info['sample'].append({'id': str(e.Id)})
        except:
            debug_info['elements'] = 0
            debug_info['sample'] = []

        poly_debug = []
        for dato in datos:
            start_point, end_point, WH1, DH, GB, DC = dato
            WH1 = float(WH1)
            DH = float(DH)
            # determine rule
            if GB.AsInteger() == 1 and DC.AsInteger() == 0 and isinstance(user_input.get('Giebelseite')[0], list):
                ecu = user_input['Giebelseite'][0]
                eq0 = ecu[0]
                eq1 = ecu[1]
                maximo = eval(eq0)
                maximo1 = eval(eq1)
                equation = eq0
                minimo = user_input['Giebelseite'][1]
            else:
                ecu = user_input.get('Traufseite')
                if ecu:
                    maximo = eval(ecu[0])
                    equation = ecu[0]
                    minimo = ecu[1]
                else:
                    maximo = WH1
                    equation = str(WH1)
                    minimo = 0

            if isinstance(maximo, (list, tuple)):
                max_val = max(maximo)
            else:
                max_val = maximo

            if max_val <= minimo:
                distancia = minimo * 3.2808
                text_min = 'AFmin = %sm' % round(minimo, 2)
            else:
                distancia = max_val * 3.2808
                text_min = ''
            text_max = 'AF = %s = %sm' % (equation, round(max_val, 2))

            direccion = Vector.ByTwoPoints(start_point, end_point).Normalized()
            perp = Vector.ByCoordinates(-direccion.Y * distancia, direccion.X * distancia, 0)
            p_start = start_point.Add(perp)
            p_end = end_point.Add(perp)
            mid = Point.ByCoordinates((start_point.X + end_point.X) / 2, (start_point.Y + end_point.Y) / 2)
            mid_new = mid.Add(perp)

            try:
                # initial polygon on the chosen side
                pc = PolyCurve.ByPoints([start_point, end_point, p_end, p_start], True)
                polyText = [pc, [mid, mid_new], text_max, text_min, DC.AsInteger()]

                # Determine which side is outside using the convex hull of building points (hull_xy)
                if hull_xy:
                    # compute opposite side
                    perp_rev = Vector.ByCoordinates(-perp.X, -perp.Y, -perp.Z)
                    p_start_rev = start_point.Add(perp_rev)
                    p_end_rev = end_point.Add(perp_rev)

                    # Use polygon containment test instead of Surface.Intersect
                    inside_a = _point_in_polygon(p_start, hull_xy) or _point_in_polygon(p_end, hull_xy)
                    inside_b = _point_in_polygon(p_start_rev, hull_xy) or _point_in_polygon(p_end_rev, hull_xy)

                    # prefer the side that is outside the surface
                    if inside_a and not inside_b:
                        p_start = p_start_rev
                        p_end = p_end_rev
                        mid_new = mid.Add(perp_rev)
                        pc = PolyCurve.ByPoints([start_point, end_point, p_end, p_start], True)
                        polyText = [pc, [mid, mid_new], text_max, text_min, DC.AsInteger()]
                    elif not inside_a:
                        # side A is already outside: keep
                        pass
                    elif not inside_b:
                        p_start = p_start_rev
                        p_end = p_end_rev
                        mid_new = mid.Add(perp_rev)
                        pc = PolyCurve.ByPoints([start_point, end_point, p_end, p_start], True)
                        polyText = [pc, [mid, mid_new], text_max, text_min, DC.AsInteger()]
                    else:
                        # both inside; choose reversed side as fallback
                        p_start = p_start_rev
                        p_end = p_end_rev
                        mid_new = mid.Add(perp_rev)
                        pc = PolyCurve.ByPoints([start_point, end_point, p_end, p_start], True)
                        polyText = [pc, [mid, mid_new], text_max, text_min, DC.AsInteger()]

                # Hybrid rule: any vertex outside => blue; else centroid decides
                flag_any_outside = False
                centroid = None
                if lot_xy:
                    poly_pts = [start_point, end_point, p_end, p_start]
                    # check vertices first
                    for v in poly_pts:
                        if not _point_in_polygon(v, lot_xy):
                            flag_any_outside = True
                            break
                    if flag_any_outside:
                        blue_lines.append(polyText)
                    else:
                        # centroid test
                        cx = sum([float(p.X) for p in poly_pts]) / len(poly_pts)
                        cy = sum([float(p.Y) for p in poly_pts]) / len(poly_pts)
                        centroid = (cx, cy)
                        centroid_inside = _point_in_polygon(centroid, lot_xy)
                        if centroid_inside:
                            red_lines.append(polyText)
                        else:
                            blue_lines.append(polyText)
                else:
                    # no lot: keep as red
                    red_lines.append(polyText)

                # record debug info per polygon (limit)
                if len(poly_debug) < 50:
                    pts_to_debug = [(float(p.X), float(p.Y)) for p in [start_point, end_point, p_start, p_end, mid_new]]
                    poly_debug.append({'pts': pts_to_debug, 'centroid': centroid, 'flag_any_outside': flag_any_outside, 'AF_text': text_max})
            except:
                pass

        # group
        red_lines_grouped = group_polycurves_by_connectivity(red_lines)
        blue_lines_grouped = group_polycurves_by_connectivity(blue_lines)

        # draw
        TransactionManager.Instance.EnsureInTransaction(doc)
        if view is None:
            TaskDialog.Show('Dynamo', 'Vista "Deckflächenplan" no encontrada')
            red_curves = []
            blue_curves = []
            red_adj = []
            blue_adj = []
        else:
            # get lot hull and xy for tests
            lot_hull, lot_xy = obtener_lote_hull()
            debug_info['lot_points'] = len(lot_hull) if lot_hull else 0
            debug_info['hull_xy_sample'] = hull_xy[:20]
            debug_info['lot_xy_sample'] = lot_xy[:20]
            debug_info['polygon_debug'] = poly_debug

            red_curves = draw_detail_curves(red_lines, style_red, lot_xy=lot_xy, style_outside=style_blue, debug_info=debug_info)
            blue_curves = draw_detail_curves(blue_lines, style_blue, lot_xy=lot_xy, style_outside=style_blue, debug_info=debug_info)
            red_adj = draw_grouped_curves(red_lines_grouped, style_red, lot_xy=lot_xy, style_outside=style_blue, debug_info=debug_info)
            blue_adj = draw_grouped_curves(blue_lines_grouped, style_blue, lot_xy=lot_xy, style_outside=style_blue, debug_info=debug_info)
        TransactionManager.Instance.TransactionTaskDone()

        return {
            'estado': 'Ejecutado terminado',
            'seleccion': {'bundesland': bundesland, 'zona': zona, 'dach': dach},
            'conteos': {'rojas': len(red_lines), 'azules': len(blue_lines), 'rojas_agrupadas': len(red_lines_grouped), 'azules_agrupadas': len(blue_lines_grouped)},
            'dibujado': {'rojas': len(red_curves), 'azules': len(blue_curves), 'rojas_adj': len(red_adj), 'azules_adj': len(blue_adj)},
            'debug_info': debug_info
        }

    except Exception as e:
        tb = traceback.format_exc()
        try:
            TaskDialog.Show('Error', 'Ocurrió error: ' + str(e))
        except:
            pass
        return {'estado': 'Error', 'error': str(e), 'traceback': tb, 'debug_info': debug_info}


try:
    OUT = main()
except Exception as e:
    tb = traceback.format_exc()
    try:
        TaskDialog.Show('Error', 'Ocurrió un error: ' + str(e))
    except:
        pass
    OUT = {'estado': 'Error', 'error': str(e), 'traceback': tb}
