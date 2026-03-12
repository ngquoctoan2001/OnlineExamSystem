import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import StudentsPage from './pages/StudentsPage'
import TeachersPage from './pages/TeachersPage'
import ClassesPage from './pages/ClassesPage'
import ClassDetailPage from './pages/ClassDetailPage'
import QuestionsPage from './pages/QuestionsPage'
import ExamsPage from './pages/ExamsPage'
import ExamDetailPage from './pages/ExamDetailPage'
import ExamPlayerPage from './pages/ExamPlayerPage'
import ResultsPage from './pages/ResultsPage'
import GradingPage from './pages/GradingPage'
import GradingDetailPage from './pages/GradingDetailPage'
import ExamReviewPage from './pages/ExamReviewPage'
import SubjectsPage from './pages/SubjectsPage'
import ActivityLogsPage from './pages/ActivityLogsPage'
import TagsPage from './pages/TagsPage'
import UsersPage from './pages/UsersPage'
import NotificationsPage from './pages/NotificationsPage'
import SystemAdminPage from './pages/SystemAdminPage'
import ProfilePage from './pages/ProfilePage'
import GradebookPage from './pages/GradebookPage'
import ReportsPage from './pages/ReportsPage'
import RegisterPage from './pages/RegisterPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import Layout from './components/Layout'

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading } = useAuth()
  if (loading) return <div className="loading-center"><div className="spinner" /></div>
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}

// Students can now access the dashboard, no redirect needed

function AppRoutes() {
  const { isAuthenticated } = useAuth()

  return (
    <Routes>
      <Route path="/login" element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route path="/register" element={isAuthenticated ? <Navigate to="/" replace /> : <RegisterPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route
        path="/"
        element={
          <PrivateRoute>
            <Layout />
          </PrivateRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="students" element={<StudentsPage />} />
        <Route path="teachers" element={<TeachersPage />} />
        <Route path="classes" element={<ClassesPage />} />
        <Route path="classes/:id" element={<ClassDetailPage />} />
        <Route path="subjects" element={<SubjectsPage />} />
        <Route path="questions" element={<QuestionsPage />} />
        <Route path="tags" element={<TagsPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="exams" element={<ExamsPage />} />
        <Route path="exams/:id" element={<ExamDetailPage />} />
        <Route path="exam-player/:examId" element={<ExamPlayerPage />} />
        <Route path="results" element={<ResultsPage />} />
        <Route path="grading" element={<GradingPage />} />
        <Route path="grading/:attemptId" element={<GradingDetailPage />} />
        <Route path="review/:attemptId" element={<ExamReviewPage />} />
        <Route path="activity-logs" element={<ActivityLogsPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route path="system-admin" element={<SystemAdminPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="gradebook" element={<GradebookPage />} />
        <Route path="reports" element={<ReportsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  )
}
