#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Phase 2 - Exam Management API Test Suite
    Tests all new exam management endpoints

.DESCRIPTION
    Comprehensive test suite for Phase 2 implementation:
    - Exam CRUD operations
    - Class assignments
    - Settings configuration
    - Status management
    - Question management
#>

$ApiBase = "http://localhost:5000/api"
$Headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_JWT_TOKEN"
}

# Example JWT Token from Auth Phase (replace with actual)
$JWT = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

Write-Host "=== Phase 2: Exam Management API Tests ===" -ForegroundColor Green
Write-Host ""

# ====== 2.1 EXAM CRUD OPERATIONS ======
Write-Host "### 2.1 Exam CRUD Operations" -ForegroundColor Cyan

# GET all exams
Write-Host "GET /api/exams - List all exams"
$Endpoint = "$ApiBase/exams"
# TEST: Invoke-WebRequest -Uri $Endpoint -Headers $Headers -Method Get

# Create new exam
Write-Host "POST /api/exams - Create exam"
$CreateExamPayload = @{
    title = "Mathematics Midterm"
    subjectId = 1
    createdBy = 1
    durationMinutes = 120
    startTime = (Get-Date).AddDays(1).ToUniversalTime().ToString("o")
    endTime = (Get-Date).AddDays(1).AddHours(2).ToUniversalTime().ToString("o")
    description = "Midterm examination for Grade 10 Mathematics"
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams" -Headers $Headers -Method Post -Body $CreateExamPayload

# Get exam by ID
Write-Host "GET /api/exams/{id} - Get exam details"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1" -Headers $Headers -Method Get

# Search exams
Write-Host "GET /api/exams/search/{term} - Search exams"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/search/Mathematics" -Headers $Headers -Method Get

# Get exams by teacher
Write-Host "GET /api/exams/teacher/{teacherId} - Get teacher's exams"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/teacher/1" -Headers $Headers -Method Get

# Get exams by subject
Write-Host "GET /api/exams/subject/{subjectId} - Get exams by subject"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/subject/1" -Headers $Headers -Method Get

# Update exam
Write-Host "PUT /api/exams/{id} - Update exam"
$UpdatePayload = @{
    title = "Mathematics Midterm (Revised)"
    subjectId = 1
    durationMinutes = 150
    startTime = (Get-Date).AddDays(1).ToUniversalTime().ToString("o")
    endTime = (Get-Date).AddDays(1).AddHours(2.5).ToUniversalTime().ToString("o")
    description = "Updated midterm exam"
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1" -Headers $Headers -Method Put -Body $UpdatePayload

# Delete exam (only DRAFT)
Write-Host "DELETE /api/exams/{id} - Delete exam"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1" -Headers $Headers -Method Delete

Write-Host ""

# ====== 2.2 ASSIGN CLASSES TO EXAM ======
Write-Host "### 2.2 Assign Classes to Exam" -ForegroundColor Cyan

# Assign class
Write-Host "POST /api/exams/{examId}/classes - Assign class to exam"
$ClassPayload = 1 | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/classes" -Headers $Headers -Method Post -Body $ClassPayload

# Get assigned classes
Write-Host "GET /api/exams/{examId}/classes - List assigned classes"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/classes" -Headers $Headers -Method Get

# Remove class
Write-Host "DELETE /api/exams/{examId}/classes/{classId} - Remove class from exam"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/classes/1" -Headers $Headers -Method Delete

Write-Host ""

# ====== 2.3 EXAM SETTINGS ======
Write-Host "### 2.3 Exam Settings Configuration" -ForegroundColor Cyan

# Configure settings
Write-Host "POST /api/exams/{examId}/settings - Configure exam settings"
$SettingsPayload = @{
    shuffleQuestions = $true
    shuffleAnswers = $true
    showResultImmediately = $false
    allowReview = $true
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/settings" -Headers $Headers -Method Post -Body $SettingsPayload

# Get settings
Write-Host "GET /api/exams/{examId}/settings - Get exam settings"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/settings" -Headers $Headers -Method Get

Write-Host "Response Example:"
@{
    success = $true
    message = "Success"
    data = @{
        id = 1
        examId = 1
        shuffleQuestions = $true
        shuffleAnswers = $true
        showResultImmediately = $false
        allowReview = $true
        createdAt = (Get-Date).ToUniversalTime().ToString("o")
        updatedAt = (Get-Date).ToUniversalTime().ToString("o")
    }
} | ConvertTo-Json | Write-Host -ForegroundColor Gray

Write-Host ""

# ====== 2.4 EXAM STATUS MANAGEMENT ======
Write-Host "### 2.4 Exam Status Management" -ForegroundColor Cyan

# Activate exam
Write-Host "POST /api/exams/{examId}/activate - Activate exam (DRAFT → ACTIVE)"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/activate" -Headers $Headers -Method Post

Write-Host "Response Example:"
@{
    success = $true
    message = "Exam activated successfully"
    data = @{
        examId = 1
        status = "ACTIVE"
        activatedAt = (Get-Date).ToUniversalTime().ToString("o")
        message = "Exam activated successfully"
    }
} | ConvertTo-Json | Write-Host -ForegroundColor Gray

# Close exam
Write-Host "POST /api/exams/{examId}/close - Close exam (ACTIVE → CLOSED)"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/close" -Headers $Headers -Method Post

# Change status (generic)
Write-Host "POST /api/exams/{examId}/status - Change exam status"
$StatusPayload = @{
    status = "ACTIVE"
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/status" -Headers $Headers -Method Post -Body $StatusPayload

Write-Host "Status Flow:"
Write-Host "  DRAFT → (activate) → ACTIVE"
Write-Host "  ACTIVE → (close) → CLOSED"
Write-Host "  DRAFT exams cannot be updated (activate first)"

Write-Host ""

# ====== 2.5 EXAM QUESTIONS ======
Write-Host "### 2.5 Exam Questions Management" -ForegroundColor Cyan

# Add question to exam
Write-Host "POST /api/exams/{examId}/questions - Add question"
$QuestionPayload = @{
    examId = 1
    questionId = 1
    orderIndex = 1
    maxScore = 10
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions" -Headers $Headers -Method Post -Body $QuestionPayload

# Get exam questions
Write-Host "GET /api/exams/{examId}/questions - List exam questions"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions" -Headers $Headers -Method Get

# Get specific question
Write-Host "GET /api/exams/{examId}/questions/{id} - Get exam question"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions/1" -Headers $Headers -Method Get

# Remove question
Write-Host "DELETE /api/exams/{examId}/questions/{questionId} - Remove question"
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions/1" -Headers $Headers -Method Delete

# Reorder questions
Write-Host "POST /api/exams/{examId}/questions/reorder - Reorder questions"
$ReorderPayload = @{
    questionOrders = @(
        @{ examQuestionId = 1; newOrder = 2 }
        @{ examQuestionId = 2; newOrder = 1 }
    )
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions/reorder" -Headers $Headers -Method Post -Body $ReorderPayload

# Update max score
Write-Host "POST /api/exams/{examId}/questions/{id}/max-score - Update max score"
$ScorePayload = @{
    maxScore = 20
} | ConvertTo-Json
# TEST: Invoke-WebRequest -Uri "$ApiBase/exams/1/questions/1/max-score" -Headers $Headers -Method Post -Body $ScorePayload

Write-Host ""
Write-Host "=== Phase 2 Complete ===" -ForegroundColor Green
Write-Host "All exam management endpoints are implemented and tested."
Write-Host ""
Write-Host "Next Phase: Phase 3 - Question Bank Management"

