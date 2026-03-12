ALTER TABLE "Classes" ADD COLUMN "HomeroomTeacherId" bigint;
ALTER TABLE "Classes" ADD CONSTRAINT "FK_Classes_Teachers_HomeroomTeacherId" FOREIGN KEY ("HomeroomTeacherId") REFERENCES "Teachers" ("Id") ON DELETE SET NULL;
CREATE INDEX "IX_Classes_HomeroomTeacherId" ON "Classes" ("HomeroomTeacherId");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260310154113_AddHomeroomTeacherToClass', '10.0.0');
