# CD-3: enum ve değer nesnelerini ızgara düzeninde (Graphviz) çizer
import html
enums = [
    ("UserRole", ["Admin", "Dietitian", "Client"]),
    ("Sex", ["Female", "Male"]),
    ("GoalType", ["LoseWeight", "Maintain", "GainWeight"]),
    ("RestrictionType", ["Allergy", "Dislike"]),
    ("PlanType", ["Daily", "Weekly"]),
    ("PlanStatus", ["Draft", "Published", "Archived"]),
    ("MealLogStatus", ["Eaten", "Partial", "Skipped"]),
    ("AppointmentStatus", ["Requested", "Confirmed", "Cancelled", "Completed"]),
    ("SubscriptionStatus", ["Active", "Expired", "Cancelled"]),
    ("NotificationType", ["AppointmentRequested", "AppointmentConfirmed", "AppointmentCancelled",
                          "AppointmentRescheduled", "AppointmentReminder", "PlanPublished",
                          "MealLogReminder", "MeasurementReminder"]),
]
# (ad, nitelikler, işlemler) ; "_" ile başlayan satır static (altı çizili)
vos = [
    ("NutrientTotals", ["+EnergyKcal: decimal", "+ProteinG: decimal", "+CarbsG: decimal", "+FatG: decimal",
                        "+/ProteinPercent: decimal", "+/CarbsPercent: decimal", "+/FatPercent: decimal",
                        "_+Zero: NutrientTotals"],
     ["+Add(other: NutrientTotals): NutrientTotals"]),
    ("TimeSlot", ["+Start: DateTime", "+End: DateTime"], ["+Overlaps(other: TimeSlot): bool"]),
    ("PhotoInfo", ["+Key: string", "+ContentType: string", "+SizeBytes: int", "_+MaxSizeBytes: int = 5242880"],
     ["_+Create(key: string, contentType: string, sizeBytes: int): PhotoInfo"]),
    ("NotificationSettings", ["+Appointments: bool", "+MealReminders: bool", "+Measurements: bool", "+Plans: bool"], []),
    ("ClientProfile", ["+FullName: string", "+Phone: string [0..1]", "+GoalType: GoalType",
                       "+TargetWeightKg: decimal [0..1]", "+Preferences: string [0..1]", "+AllergyNotes: string [0..1]"], []),
]

def line(s):
    if s.startswith("_"):
        return f'<U>{html.escape(s[1:])}</U>'
    return html.escape(s)

def box(name, stereo, attrs, ops):
    rows = [f'<TR><TD BGCOLOR="#eeeeee"><FONT POINT-SIZE="10">«{stereo}»</FONT><BR/><B>{html.escape(name)}</B></TD></TR>']
    body = '<BR ALIGN="LEFT"/>'.join(line(a) for a in attrs) + '<BR ALIGN="LEFT"/>' if attrs else " "
    rows.append(f'<TR><TD BGCOLOR="#f7f7f7" ALIGN="LEFT" BALIGN="LEFT">{body}</TD></TR>')
    if stereo != "enumeration":
        body2 = '<BR ALIGN="LEFT"/>'.join(line(o) for o in ops) + '<BR ALIGN="LEFT"/>' if ops else " "
        rows.append(f'<TR><TD BGCOLOR="#f7f7f7" ALIGN="LEFT" BALIGN="LEFT">{body2}</TD></TR>')
    return f'<<TABLE BORDER="1" CELLBORDER="1" CELLSPACING="0" CELLPADDING="6" COLOR="#999999">{"".join(rows)}</TABLE>>'

out = ['digraph G {', ' graph [rankdir=TB, nodesep=0.35, ranksep=0.45, fontname="DejaVu Sans"];',
       ' node [shape=plaintext, fontname="DejaVu Sans", fontsize=11];', ' edge [style=invis];']
rows = [enums[:5], enums[5:], vos]
for r, items in enumerate(rows):
    names = []
    for item in items:
        if r < 2:
            n, vals = item
            out.append(f' {n} [label={box(n, "enumeration", vals, [])}];')
        else:
            n, a, o = item
            out.append(f' {n} [label={box(n, "value object", a, o)}];')
        names.append(n)
    out.append(' { rank=same; ' + '; '.join(names) + '; }')
    out.append(' ' + ' -> '.join(names) + ';')
# satırları alt alta diz
out.append(f' {rows[0][0][0]} -> {rows[1][0][0]} -> {rows[2][0][0]};')
out.append('}')
open(__import__('os').path.join(__import__('os').path.dirname(__file__), '..', 'cd-3-enum-deger-nesneleri.dot'), 'w', encoding='utf-8').write('\n'.join(out))
