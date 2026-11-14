# P-6 Repository deseni: Graphviz ile düzenli yerleşim (diğer repository arayüzleri ızgara halinde)
import html, os

def esc(s):
    return html.escape(s).replace('*', '&#42;')

def box(name, stereo, attrs, ops):
    head = (f'<FONT POINT-SIZE="10">«{esc(stereo)}»</FONT><BR/>' if stereo else '') + f'<B>{esc(name)}</B>'
    rows = [f'<TR><TD BGCOLOR="#eeeeee">{head}</TD></TR>']
    for comp in (attrs, ops):
        body = '<BR ALIGN="LEFT"/>'.join(esc(x) for x in comp) + '<BR ALIGN="LEFT"/>' if comp else ' '
        rows.append(f'<TR><TD BGCOLOR="#f7f7f7" ALIGN="LEFT" BALIGN="LEFT">{body}</TD></TR>')
    return f'<<TABLE BORDER="1" CELLBORDER="1" CELLSPACING="0" CELLPADDING="6" COLOR="#999999">{"".join(rows)}</TABLE>>'

R = 'interface · Repository'
nodes = {
    'ClientService': ('Application servisi', [], ['+AddClientAsync(cmd: AddClientCommand): Guid',
                                                  '+UpdateClientAsync(clientId: Guid, profile: ClientProfile): void']),
    'IClientRepository': (R, [], ['+GetAsync(dietitianId: Guid, clientId: Guid): Client [0..1]',
                                  '+ListAsync(dietitianId: Guid, onlyActive: bool): Client[0..*]',
                                  '+CountActiveAsync(dietitianId: Guid): int',
                                  '+Add(client: Client): void', '+Remove(client: Client): void']),
    'ISubscriptionRepository': (R, [], ['+GetActiveForUpdateAsync(dietitianId: Guid): Subscription [0..1]',
                                        '+GetCurrentAsync(dietitianId: Guid): Subscription [0..1]',
                                        '+ListDueForExpiryAsync(today: DateOnly): Subscription[0..*]',
                                        '+Add(subscription: Subscription): void']),
    'IUnitOfWork': ('interface', [], ['+SaveChangesAsync(): int', '+ExecuteInTransactionAsync(work: Func<Task>): void']),
    'EfClientRepository': ('', [], ['+GetAsync(dietitianId: Guid, clientId: Guid): Client [0..1]',
                                    '+CountActiveAsync(dietitianId: Guid): int', '…']),
    'EfSubscriptionRepository': ('', [], ['+GetActiveForUpdateAsync(dietitianId: Guid): Subscription [0..1]', '…']),
    'EfUnitOfWork': ('', [], ['+SaveChangesAsync(): int', '+ExecuteInTransactionAsync(work: Func<Task>): void']),
    'DdtsDbContext': ('EF Core DbContext', [], []),
    'INutritionPlanRepository': (R, [], ['+GetAsync(dietitianId: Guid, planId: Guid): NutritionPlan [0..1]',
                                         '+GetPublishedForClientAsync(clientId: Guid): NutritionPlan [0..1]',
                                         '+Add(plan: NutritionPlan): void']),
    'IPlanTemplateRepository': (R, [], ['+GetAsync(dietitianId: Guid, templateId: Guid): PlanTemplate [0..1]',
                                        '+Add(template: PlanTemplate): void']),
    'IFoodItemRepository': (R, [], ['+GetManyAsync(dietitianId: Guid, ids: Guid[0..*]): FoodItem[0..*]',
                                    '+Add(food: FoodItem): void']),
    'IMeasurementRepository': (R, [], ['+ListAsync(clientId: Guid, from: DateOnly, to: DateOnly): Measurement[0..*]',
                                       '+Add(measurement: Measurement): void']),
    'IMealLogRepository': (R, [], ['+ListAsync(clientId: Guid, from: DateOnly, to: DateOnly): MealLog[0..*]',
                                   '+Add(log: MealLog): void']),
    'IAppointmentRepository': (R, [], ['+GetAsync(dietitianId: Guid, appointmentId: Guid): Appointment [0..1]',
                                       '+ListActiveAsync(dietitianId: Guid, date: DateOnly): Appointment[0..*]',
                                       '+Add(appointment: Appointment): void']),
    'IUserRepository': (R, [], ['+GetAsync(userId: Guid): User [0..1]',
                                '+GetByEmailAsync(email: string): User [0..1]',
                                '+GetByResetTokenHashAsync(tokenHash: string): User [0..1]',
                                '+EmailExistsAsync(email: string): bool',
                                '+GetManyAsync(userIds: Guid[0..*]): User[0..*]',
                                '+Add(user: User): void',
                                '+Remove(user: User): void']),
    'IDietitianRepository': (R, [], ['+GetAsync(dietitianId: Guid): Dietitian [0..1]',
                                     '+ListAsync(page: int, pageSize: int): Dietitian[0..*]',
                                     '+CountAsync(): int',
                                     '+Add(dietitian: Dietitian): void']),
    'ISubscriptionPlanRepository': (R, [], ['+GetAsync(subscriptionPlanId: Guid): SubscriptionPlan [0..1]',
                                            '+ListActiveAsync(): SubscriptionPlan[0..*]',
                                            '+ListAllAsync(): SubscriptionPlan[0..*]',
                                            '+Add(plan: SubscriptionPlan): void']),
    'IAuditLogRepository': (R, [], ['+ListForEntityAsync(entityType: string, entityId: Guid): AuditLog[0..*]',
                                    '+ListAsync(filter: AuditLogFilter, page: int, pageSize: int): AuditLog[0..*]',
                                    '+CountAsync(filter: AuditLogFilter): int']),
}
note_style = 'shape=note, style=filled, fillcolor="#fff8dc", color="#999999", fontsize=10'
out = ['digraph G {',
       ' graph [rankdir=TB, nodesep=0.4, ranksep=0.55, fontname="DejaVu Sans", compound=true];',
       ' node [shape=plaintext, fontname="DejaVu Sans", fontsize=11];',
       ' edge [fontname="DejaVu Sans", fontsize=9, color="#555555"];']
main = ['ClientService', 'IClientRepository', 'ISubscriptionRepository', 'IUnitOfWork',
        'EfClientRepository', 'EfSubscriptionRepository', 'EfUnitOfWork', 'DdtsDbContext']
for n in main:
    s, a, o = nodes[n]
    out.append(f' {n} [label={box(n, s, a, o)}];')
out.append(' { rank=same; IClientRepository; ISubscriptionRepository; IUnitOfWork; }')
out.append(' { rank=same; EfClientRepository; EfSubscriptionRepository; EfUnitOfWork; }')
out.append(' subgraph cluster_other {')
out.append('  label="Diğer repository arayüzleri (EF Core uygulamaları aynı kalıpta: EfNutritionPlanRepository, EfPlanTemplateRepository, …)";')
out.append('  labeljust=l; style="rounded,dashed"; color="#888888"; fontsize=10; fontname="DejaVu Sans";')
grid = [['INutritionPlanRepository', 'IPlanTemplateRepository', 'IFoodItemRepository'],
        ['IMeasurementRepository', 'IMealLogRepository', 'IAppointmentRepository'],
        ['IUserRepository', 'IDietitianRepository', 'ISubscriptionPlanRepository'],
        ['IAuditLogRepository']]
for row in grid:
    for n in row:
        s, a, o = nodes[n]
        out.append(f'  {n} [label={box(n, s, a, o)}];')
    if len(row) > 1:
        out.append('  { rank=same; ' + '; '.join(row) + '; }')
        out.append('  ' + ' -> '.join(row) + ' [style=invis];')
for r in range(len(grid) - 1):
    for i in range(min(len(grid[r]), len(grid[r + 1]))):
        out.append(f'  {grid[r][i]} -> {grid[r + 1][i]} [style=invis];')
out.append(' }')
# notlar
out.append(f' n1 [{note_style}, label="Arayüzler Application katmanında (port).\\nDiyetisyen verisi okuyan her metot dietitianId alır:\\nbaşka diyetisyenin kaydı hiç dönmez (IDOR koruması)."];')
out.append(f' n2 [{note_style}, label="SELECT … FOR UPDATE: aynı anda iki danışan\\neklenirken limit aşılmasın diye satır kilitlenir."];')
out.append(' n1 -> IClientRepository [style=dashed, arrowhead=none, color="#999999"];')
out.append(' n2 -> EfSubscriptionRepository [style=dashed, arrowhead=none, color="#999999"];')
out.append(' { rank=same; ClientService; n1; n2; }')
# ilişkiler
for t in ['IClientRepository', 'ISubscriptionRepository', 'IUnitOfWork']:
    out.append(f' ClientService -> {t} [arrowhead=vee];')
for impl, itf in [('EfClientRepository', 'IClientRepository'), ('EfSubscriptionRepository', 'ISubscriptionRepository'),
                  ('EfUnitOfWork', 'IUnitOfWork')]:
    out.append(f' {itf} -> {impl} [style=dashed, dir=back, arrowtail=onormal, arrowsize=1.2];')
for impl in ['EfClientRepository', 'EfSubscriptionRepository', 'EfUnitOfWork']:
    out.append(f' {impl} -> DdtsDbContext [arrowhead=vee];')
# diğer arayüz grubunu ana sütunun yanına yerleştir
out.append(' ClientService -> INutritionPlanRepository [style=invis];')
out.append('}')
here = os.path.dirname(os.path.abspath(__file__))
dst = os.path.join(here, '..', 'src', 'p-6-repository.dot')
if not os.path.isdir(os.path.join(here, '..', 'src')):
    dst = os.path.join(here, '..', 'p-6-repository.dot')
open(dst, 'w', encoding='utf-8').write('\n'.join(out))
print('yazıldı:', os.path.normpath(dst))
