Import-Module ImportExcel

$outDir = $PSScriptRoot

# ════════════════════════════════════════════════════════
# 1. TEACHERS (20 giáo viên)
# Columns: EmployeeCode, FirstName, LastName, Email, PhoneNumber, Department
# ════════════════════════════════════════════════════════

$teachers = @(
    # Toán học (3)
    @{ EmployeeCode="GV001"; FirstName="Nguyễn Văn"; LastName="An";    Email="gv001@school.edu"; PhoneNumber="0901000001"; Department="Toán học" }
    @{ EmployeeCode="GV002"; FirstName="Trần Thị";   LastName="Bình";  Email="gv002@school.edu"; PhoneNumber="0901000002"; Department="Toán học" }
    @{ EmployeeCode="GV003"; FirstName="Lê Hoàng";   LastName="Cường"; Email="gv003@school.edu"; PhoneNumber="0901000003"; Department="Toán học" }
    # Vật lý (2)
    @{ EmployeeCode="GV004"; FirstName="Phạm Minh";  LastName="Đức";   Email="gv004@school.edu"; PhoneNumber="0901000004"; Department="Vật lý" }
    @{ EmployeeCode="GV005"; FirstName="Hoàng Thị";  LastName="Hoa";   Email="gv005@school.edu"; PhoneNumber="0901000005"; Department="Vật lý" }
    # Hóa học (2)
    @{ EmployeeCode="GV006"; FirstName="Vũ Đình";    LastName="Khoa";  Email="gv006@school.edu"; PhoneNumber="0901000006"; Department="Hóa học" }
    @{ EmployeeCode="GV007"; FirstName="Đặng Thị";   LastName="Lan";   Email="gv007@school.edu"; PhoneNumber="0901000007"; Department="Hóa học" }
    # Sinh học (1)
    @{ EmployeeCode="GV008"; FirstName="Bùi Quang";  LastName="Minh";  Email="gv008@school.edu"; PhoneNumber="0901000008"; Department="Sinh học" }
    # Ngữ văn (2)
    @{ EmployeeCode="GV009"; FirstName="Ngô Thị";    LastName="Ngọc";  Email="gv009@school.edu"; PhoneNumber="0901000009"; Department="Ngữ văn" }
    @{ EmployeeCode="GV010"; FirstName="Dương Văn";   LastName="Phúc";  Email="gv010@school.edu"; PhoneNumber="0901000010"; Department="Ngữ văn" }
    # Tiếng Anh (2)
    @{ EmployeeCode="GV011"; FirstName="Lý Thị";     LastName="Quỳnh"; Email="gv011@school.edu"; PhoneNumber="0901000011"; Department="Tiếng Anh" }
    @{ EmployeeCode="GV012"; FirstName="Trương Minh"; LastName="Sơn";   Email="gv012@school.edu"; PhoneNumber="0901000012"; Department="Tiếng Anh" }
    # Lịch sử (1)
    @{ EmployeeCode="GV013"; FirstName="Phan Văn";    LastName="Tài";   Email="gv013@school.edu"; PhoneNumber="0901000013"; Department="Lịch sử" }
    # Địa lý (1)
    @{ EmployeeCode="GV014"; FirstName="Mai Thị";     LastName="Uyên";  Email="gv014@school.edu"; PhoneNumber="0901000014"; Department="Địa lý" }
    # GDKTPL (1)
    @{ EmployeeCode="GV015"; FirstName="Hồ Trọng";   LastName="Vinh";  Email="gv015@school.edu"; PhoneNumber="0901000015"; Department="Giáo dục Kinh tế & Pháp luật" }
    # GDQPAN (1)
    @{ EmployeeCode="GV016"; FirstName="Đinh Công";   LastName="Tuấn";  Email="gv016@school.edu"; PhoneNumber="0901000016"; Department="Giáo dục Quốc phòng & An Ninh" }
    # Thể dục (1)
    @{ EmployeeCode="GV017"; FirstName="Cao Văn";     LastName="Hùng";  Email="gv017@school.edu"; PhoneNumber="0901000017"; Department="Thể dục" }
    # Công nghệ (1)
    @{ EmployeeCode="GV018"; FirstName="Tô Thị";      LastName="Hạnh";  Email="gv018@school.edu"; PhoneNumber="0901000018"; Department="Công nghệ" }
    # Tin học phụ trách thêm (2)
    @{ EmployeeCode="GV019"; FirstName="Lương Đức";   LastName="Thắng"; Email="gv019@school.edu"; PhoneNumber="0901000019"; Department="Toán học" }
    @{ EmployeeCode="GV020"; FirstName="Châu Thị";    LastName="Yến";   Email="gv020@school.edu"; PhoneNumber="0901000020"; Department="Ngữ văn" }
)

$teachers | ForEach-Object { [PSCustomObject]$_ } |
    Export-Excel -Path "$outDir\teachers.xlsx" -WorksheetName "Teachers" -AutoSize -FreezeTopRow
Write-Host "Created: teachers.xlsx (20 teachers)"

# ════════════════════════════════════════════════════════
# 2. STUDENTS (60 học sinh - phân bổ vào 9 lớp)
# Columns: StudentCode, FirstName, LastName, Email, PhoneNumber, ClassName, DateOfBirth
# ════════════════════════════════════════════════════════

# 9 lớp: 10A1, 10A2, 10A3, 11A1, 11A2, 11A3, 12A1, 12A2, 12A3
# Mỗi lớp khoảng 6-7 học sinh

$lastNames = @("Nguyễn","Trần","Lê","Phạm","Hoàng","Vũ","Đặng","Bùi","Ngô","Dương","Lý","Phan","Mai","Hồ","Đinh","Cao","Tô","Lương","Châu","Đỗ")
$maleMiddle = @("Văn","Minh","Quang","Đức","Hoàng","Trọng","Công","Hữu","Đình","Xuân")
$femaleMiddle = @("Thị","Ngọc","Thanh","Kim","Bích","Phương","Hồng","Tuyết","Diệu","Mỹ")
$maleFirst = @("Hùng","Dũng","Tuấn","Quân","Khôi","Long","Phát","Tâm","Đạt","Bảo","Kiên","Thịnh","Khang","Trí","Lộc","Nam","Hiếu","Phong","Khánh","Anh")
$femaleFirst = @("Linh","Hà","Trang","Thảo","Vy","Ngân","Nhi","Trinh","Hương","Mai","Châu","Yến","Uyên","Quyên","Thư","Duyên","Hạnh","Tiên","Nhung","Thy")

$classes = @("10A1","10A2","10A3","11A1","11A2","11A3","12A1","12A2","12A3")

$students = @()
$hsIdx = 1

foreach ($cls in $classes) {
    # Each class gets ~7 students (total: 63 -> trim to 60)
    $count = if ($hsIdx -le 54) { 7 } else { 6 }
    if ($hsIdx -gt 60) { break }

    for ($i = 0; $i -lt $count; $i++) {
        if ($hsIdx -gt 60) { break }

        $code = "HS{0:D3}" -f $hsIdx
        $isMale = ($hsIdx % 2 -eq 1)
        $surname = $lastNames[($hsIdx - 1) % $lastNames.Count]

        if ($isMale) {
            $middle = $maleMiddle[($hsIdx - 1) % $maleMiddle.Count]
            $given  = $maleFirst[($hsIdx - 1) % $maleFirst.Count]
        } else {
            $middle = $femaleMiddle[($hsIdx - 1) % $femaleMiddle.Count]
            $given  = $femaleFirst[($hsIdx - 1) % $femaleFirst.Count]
        }

        # Determine grade year for DOB
        $gradeNum = [int]($cls -replace '[^\d]','')[0]
        # Grade 10 -> born ~2010, Grade 11 -> born ~2009, Grade 12 -> born ~2008
        $birthYear = 2026 - 6 - $gradeNum  # roughly: 16 for gr10, 17 for gr11, 18 for gr12
        $birthMonth = (($hsIdx * 3) % 12) + 1
        $birthDay = (($hsIdx * 7) % 28) + 1
        $dob = Get-Date -Year $birthYear -Month $birthMonth -Day $birthDay -Format "yyyy-MM-dd"

        $students += [PSCustomObject]@{
            StudentCode = $code
            FirstName   = "$surname $middle"
            LastName    = $given
            Email       = "$($code.ToLower())@school.edu"
            PhoneNumber = "090200{0:D4}" -f $hsIdx
            ClassName   = $cls
            DateOfBirth = $dob
        }

        $hsIdx++
    }
}

$students | Export-Excel -Path "$outDir\students.xlsx" -WorksheetName "Students" -AutoSize -FreezeTopRow
Write-Host "Created: students.xlsx ($($students.Count) students across $($classes.Count) classes)"

Write-Host ""
Write-Host "=== SEED DATA FILES CREATED ==="
Write-Host "  1. teachers.xlsx  - 20 giáo viên"
Write-Host "  2. students.xlsx  - $($students.Count) học sinh (9 lớp)"
Write-Host ""
Write-Host "=== CLASS DISTRIBUTION ==="
$students | Group-Object ClassName | Format-Table Name, Count -AutoSize
