using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ddts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_clients = table.Column<int>(type: "integer", nullable: false),
                    monthly_price = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_plans", x => x.id);
                    table.CheckConstraint("ck_subscription_plans_max_clients", "max_clients > 0");
                    table.CheckConstraint("ck_subscription_plans_monthly_price", "monthly_price >= 0");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    push_token = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reset_token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reset_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_email_lower", "email = lower(email)");
                    table.CheckConstraint("ck_users_reset_token", "(reset_token_hash IS NULL) = (reset_token_expires_at IS NULL)");
                    table.CheckConstraint("ck_users_role", "role IN ('Admin', 'Dietitian', 'Client')");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "dietitians",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dietitians", x => x.id);
                    table.ForeignKey(
                        name: "fk_dietitians_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    body = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dedup_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.CheckConstraint("ck_notifications_type", "type IN (\n    'AppointmentRequested', 'AppointmentConfirmed', 'AppointmentCancelled',\n    'AppointmentRescheduled', 'AppointmentReminder',\n    'PlanPublished', 'MealLogReminder', 'MeasurementReminder')");
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sex = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    goal_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    target_weight_kg = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    preferences = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    allergy_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    kvkk_consent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    notify_appointments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_meal_reminders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_measurements = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_plans = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                    table.UniqueConstraint("ak_clients_id_dietitian_id", x => new { x.id, x.dietitian_id });
                    table.CheckConstraint("ck_clients_goal_type", "goal_type IN ('LoseWeight', 'Maintain', 'GainWeight')");
                    table.CheckConstraint("ck_clients_sex", "sex IN ('Female', 'Male')");
                    table.CheckConstraint("ck_clients_target_weight", "target_weight_kg BETWEEN 20 AND 350");
                    table.ForeignKey(
                        name: "fk_clients_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_clients_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "food_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    portion_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    portion_grams = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                    energy_kcal_per_100g = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                    protein_g_per_100g = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    carbs_g_per_100g = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    fat_g_per_100g = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    source_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Yerel"),
                    external_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_food_items", x => x.id);
                    table.CheckConstraint("ck_food_items_carbs", "carbs_g_per_100g BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_food_items_energy", "energy_kcal_per_100g BETWEEN 0 AND 900");
                    table.CheckConstraint("ck_food_items_external_source", "(external_id IS NULL) = (source_updated_at IS NULL)");
                    table.CheckConstraint("ck_food_items_fat", "fat_g_per_100g BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_food_items_macro_total", "protein_g_per_100g + carbs_g_per_100g + fat_g_per_100g <= 100");
                    table.CheckConstraint("ck_food_items_portion_grams", "portion_grams > 0");
                    table.CheckConstraint("ck_food_items_protein", "protein_g_per_100g BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "fk_food_items_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    plan_type = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_templates", x => x.id);
                    table.CheckConstraint("ck_plan_templates_plan_type", "plan_type IN ('Daily', 'Weekly')");
                    table.ForeignKey(
                        name: "fk_plan_templates_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.CheckConstraint("ck_subscriptions_dates", "start_date < end_date");
                    table.CheckConstraint("ck_subscriptions_status", "status IN ('Active', 'Expired', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_subscriptions_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subscriptions_subscription_plans_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "working_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    weekday = table.Column<short>(type: "smallint", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_working_hours", x => x.id);
                    table.CheckConstraint("ck_working_hours_time_range", "start_time < end_time");
                    table.CheckConstraint("ck_working_hours_weekday", "weekday BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "fk_working_hours_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.CheckConstraint("ck_appointments_status", "status IN ('Requested', 'Confirmed', 'Cancelled', 'Completed')");
                    table.CheckConstraint("ck_appointments_time_range", "starts_at < ends_at");
                    table.ForeignKey(
                        name: "fk_appointments_clients_client_id_dietitian_id",
                        columns: x => new { x.client_id, x.dietitian_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "dietitian_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_appointments_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "measurements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    measured_on = table.Column<DateOnly>(type: "date", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    waist_cm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    hip_cm = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_measurements", x => x.id);
                    table.CheckConstraint("ck_measurements_body_fat", "body_fat_percent BETWEEN 2 AND 75");
                    table.CheckConstraint("ck_measurements_height", "height_cm BETWEEN 100 AND 250");
                    table.CheckConstraint("ck_measurements_hip", "hip_cm BETWEEN 30 AND 250");
                    table.CheckConstraint("ck_measurements_waist", "waist_cm BETWEEN 30 AND 250");
                    table.CheckConstraint("ck_measurements_weight", "weight_kg BETWEEN 20 AND 350");
                    table.ForeignKey(
                        name: "fk_measurements_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_food_restrictions",
                columns: table => new
                {
                    food_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_food_restrictions", x => new { x.client_id, x.food_item_id });
                    table.CheckConstraint("ck_client_food_restrictions_type", "type IN ('Allergy', 'Dislike')");
                    table.ForeignKey(
                        name: "fk_client_food_restrictions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_food_restrictions_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nutrition_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dietitian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    plan_type = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'Draft'"),
                    target_kcal = table.Column<int>(type: "integer", nullable: true),
                    energy_formula = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nutrition_plans", x => x.id);
                    table.CheckConstraint("ck_nutrition_plans_plan_type", "plan_type IN ('Daily', 'Weekly')");
                    table.CheckConstraint("ck_nutrition_plans_status", "status IN ('Draft', 'Published', 'Archived')");
                    table.CheckConstraint("ck_nutrition_plans_status_dates", "(status = 'Draft'     AND published_at IS NULL     AND archived_at IS NULL) OR\n(status = 'Published' AND published_at IS NOT NULL AND archived_at IS NULL) OR\n(status = 'Archived'  AND published_at IS NOT NULL AND archived_at IS NOT NULL)");
                    table.CheckConstraint("ck_nutrition_plans_target_kcal", "target_kcal BETWEEN 800 AND 6000");
                    table.ForeignKey(
                        name: "fk_nutrition_plans_clients_client_id_dietitian_id",
                        columns: x => new { x.client_id, x.dietitian_id },
                        principalTable: "clients",
                        principalColumns: new[] { "id", "dietitian_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_nutrition_plans_dietitians_dietitian_id",
                        column: x => x.dietitian_id,
                        principalTable: "dietitians",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_nutrition_plans_plan_templates_source_template_id",
                        column: x => x.source_template_id,
                        principalTable: "plan_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "meals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    day_no = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    meal_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meals", x => x.id);
                    table.CheckConstraint("ck_meals_day_no", "day_no BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_meals_owner", "num_nonnulls(plan_id, template_id) = 1");
                    table.ForeignKey(
                        name: "fk_meals_nutrition_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "nutrition_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_meals_plan_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "plan_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    food_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portion_count = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    alternative_to_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_items", x => x.id);
                    table.UniqueConstraint("ak_meal_items_id_meal_id", x => new { x.id, x.meal_id });
                    table.CheckConstraint("ck_meal_items_not_self", "alternative_to_item_id IS NULL OR alternative_to_item_id <> id");
                    table.CheckConstraint("ck_meal_items_portion_count", "portion_count > 0 AND portion_count <= 20");
                    table.ForeignKey(
                        name: "fk_meal_items_food_items_food_item_id",
                        column: x => x.food_item_id,
                        principalTable: "food_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_meal_items_meal_items_alternative_to_item_id_meal_id",
                        columns: x => new { x.alternative_to_item_id, x.meal_id },
                        principalTable: "meal_items",
                        principalColumns: new[] { "id", "meal_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_meal_items_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meal_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    photo_content_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    photo_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    photo_size_bytes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_logs", x => x.id);
                    table.CheckConstraint("ck_meal_logs_photo_complete", "num_nonnulls(photo_key, photo_content_type, photo_size_bytes) IN (0, 3)");
                    table.CheckConstraint("ck_meal_logs_photo_content_type", "photo_content_type IN ('image/jpeg', 'image/png')");
                    table.CheckConstraint("ck_meal_logs_photo_size", "photo_size_bytes BETWEEN 1 AND 5242880");
                    table.CheckConstraint("ck_meal_logs_status", "status IN ('Eaten', 'Partial', 'Skipped')");
                    table.ForeignKey(
                        name: "fk_meal_logs_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_meal_logs_meals_meal_id",
                        column: x => x.meal_id,
                        principalTable: "meals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "id", "is_active", "max_clients", "monthly_price", "name" },
                values: new object[,]
                {
                    { new Guid("019a7f00-0000-7000-8000-000000000001"), true, 10, 299m, "Başlangıç" },
                    { new Guid("019a7f00-0000-7000-8000-000000000002"), true, 30, 599m, "Standart" },
                    { new Guid("019a7f00-0000-7000-8000-000000000003"), true, 100, 999m, "Profesyonel" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_client",
                table: "appointments",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_dietitian",
                table: "clients",
                column: "dietitian_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_user_id",
                table: "clients",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dietitians_user_id",
                table: "dietitians",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_food_items_external",
                table: "food_items",
                columns: new[] { "dietitian_id", "source_name", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_meal_items_meal",
                table: "meal_items",
                column: "meal_id");

            migrationBuilder.CreateIndex(
                name: "ix_meal_logs_client_date",
                table: "meal_logs",
                columns: new[] { "client_id", "log_date" });

            migrationBuilder.CreateIndex(
                name: "ix_meal_logs_client_id_meal_id_log_date",
                table: "meal_logs",
                columns: new[] { "client_id", "meal_id", "log_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meals_plan",
                table: "meals",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_meals_template",
                table: "meals",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_measurements_client_id_measured_on",
                table: "measurements",
                columns: new[] { "client_id", "measured_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_dedup_key",
                table: "notifications",
                column: "dedup_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_nutrition_plans_dietitian",
                table: "nutrition_plans",
                column: "dietitian_id");

            migrationBuilder.CreateIndex(
                name: "ux_nutrition_plans_one_published",
                table: "nutrition_plans",
                column: "client_id",
                unique: true,
                filter: "status = 'Published'");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_name",
                table: "subscription_plans",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_one_active",
                table: "subscriptions",
                column: "dietitian_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_working_hours_dietitian_id_weekday",
                table: "working_hours",
                columns: new[] { "dietitian_id", "weekday" },
                unique: true);
            ManualSqlConstraints.Create(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ManualSqlConstraints.Drop(migrationBuilder);
            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "client_food_restrictions");

            migrationBuilder.DropTable(
                name: "meal_items");

            migrationBuilder.DropTable(
                name: "meal_logs");

            migrationBuilder.DropTable(
                name: "measurements");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "working_hours");

            migrationBuilder.DropTable(
                name: "food_items");

            migrationBuilder.DropTable(
                name: "meals");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "nutrition_plans");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "plan_templates");

            migrationBuilder.DropTable(
                name: "dietitians");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
