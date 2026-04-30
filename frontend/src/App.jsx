import { useEffect, useMemo, useState } from 'react'
import './App.css'

const API_BASE_URL = ''
const STATUS_FLOW = ['Pending', 'In Progress', 'Completed', 'Blocked']
const PRIORITIES = ['Low', 'Medium', 'High', 'Critical']

async function apiRequest(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

function formatDate(value) {
  return new Intl.DateTimeFormat('en', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function App() {
  const [health, setHealth] = useState(null)
  const [tasks, setTasks] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const [form, setForm] = useState({
    title: '',
    description: '',
    priority: 'Medium',
  })

  const taskCounts = useMemo(() => {
    return tasks.reduce(
      (counts, task) => {
        counts.total += 1
        counts[task.status] = (counts[task.status] ?? 0) + 1
        return counts
      },
      { total: 0 },
    )
  }, [tasks])

  async function loadDashboard() {
    try {
      setError('')
      const [healthResult, taskResult] = await Promise.all([
        apiRequest('/health'),
        apiRequest('/api/tasks'),
      ])

      setHealth(healthResult)
      setTasks(taskResult)
    } catch (requestError) {
      setError(
        'Could not reach the backend API. Start Docker Compose and try again.',
      )
      console.error(requestError)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    let isActive = true

    async function loadInitialDashboard() {
      try {
        const [healthResult, taskResult] = await Promise.all([
          apiRequest('/health'),
          apiRequest('/api/tasks'),
        ])

        if (!isActive) {
          return
        }

        setHealth(healthResult)
        setTasks(taskResult)
      } catch (requestError) {
        if (!isActive) {
          return
        }

        setError(
          'Could not reach the backend API. Start Docker Compose and try again.',
        )
        console.error(requestError)
      } finally {
        if (isActive) {
          setIsLoading(false)
        }
      }
    }

    loadInitialDashboard()

    return () => {
      isActive = false
    }
  }, [])

  function updateForm(event) {
    const { name, value } = event.target
    setForm((currentForm) => ({
      ...currentForm,
      [name]: value,
    }))
  }

  async function createTask(event) {
    event.preventDefault()

    if (!form.title.trim()) {
      setError('Task title is required.')
      return
    }

    try {
      setIsSaving(true)
      setError('')

      const task = await apiRequest('/api/tasks', {
        method: 'POST',
        body: JSON.stringify({
          title: form.title.trim(),
          description: form.description.trim(),
          priority: form.priority,
        }),
      })

      setTasks((currentTasks) => [...currentTasks, task])
      setForm({
        title: '',
        description: '',
        priority: 'Medium',
      })
    } catch (requestError) {
      setError('Could not create the task.')
      console.error(requestError)
    } finally {
      setIsSaving(false)
    }
  }

  async function cycleStatus(task) {
    const currentIndex = STATUS_FLOW.indexOf(task.status)
    const nextStatus = STATUS_FLOW[(currentIndex + 1) % STATUS_FLOW.length]

    try {
      setError('')
      const updatedTask = await apiRequest(`/api/tasks/${task.id}`, {
        method: 'PUT',
        body: JSON.stringify({ status: nextStatus }),
      })

      setTasks((currentTasks) =>
        currentTasks.map((currentTask) =>
          currentTask.id === updatedTask.id ? updatedTask : currentTask,
        ),
      )
    } catch (requestError) {
      setError('Could not update task status.')
      console.error(requestError)
    }
  }

  async function deleteTask(taskId) {
    try {
      setError('')
      await apiRequest(`/api/tasks/${taskId}`, {
        method: 'DELETE',
      })

      setTasks((currentTasks) =>
        currentTasks.filter((task) => task.id !== taskId),
      )
    } catch (requestError) {
      setError('Could not delete the task.')
      console.error(requestError)
    }
  }

  return (
    <main className="app-shell">
      <section className="hero-section">
        <div>
          <p className="eyebrow">LocalDeploy Lab</p>
          <h1>DevOps Task Manager</h1>
          <p className="intro">
            Track lab tasks while the React frontend talks to the ASP.NET Core
            API and PostgreSQL backend.
          </p>
        </div>

        <div className="status-panel" aria-label="System status">
          <div>
            <span className="status-label">API</span>
            <strong className={health?.status === 'running' ? 'ok' : 'warn'}>
              {health?.status ?? 'checking'}
            </strong>
          </div>
          <div>
            <span className="status-label">Database</span>
            <strong className={health?.database === 'connected' ? 'ok' : 'warn'}>
              {health?.database ?? 'checking'}
            </strong>
          </div>
        </div>
      </section>

      {error && <div className="notice">{error}</div>}

      <section className="summary-grid" aria-label="Task summary">
        <div>
          <span>Total</span>
          <strong>{taskCounts.total}</strong>
        </div>
        <div>
          <span>Pending</span>
          <strong>{taskCounts.Pending ?? 0}</strong>
        </div>
        <div>
          <span>In Progress</span>
          <strong>{taskCounts['In Progress'] ?? 0}</strong>
        </div>
        <div>
          <span>Completed</span>
          <strong>{taskCounts.Completed ?? 0}</strong>
        </div>
      </section>

      <section className="workspace">
        <form className="task-form" onSubmit={createTask}>
          <h2>Create Task</h2>
          <label>
            Title
            <input
              name="title"
              type="text"
              value={form.title}
              onChange={updateForm}
              placeholder="Add Dockerized frontend"
            />
          </label>
          <label>
            Description
            <textarea
              name="description"
              value={form.description}
              onChange={updateForm}
              placeholder="What needs to be done?"
              rows="4"
            />
          </label>
          <label>
            Priority
            <select name="priority" value={form.priority} onChange={updateForm}>
              {PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {priority}
                </option>
              ))}
            </select>
          </label>
          <button type="submit" className="primary-action" disabled={isSaving}>
            {isSaving ? 'Creating...' : 'Create Task'}
          </button>
        </form>

        <section className="task-list" aria-label="Tasks">
          <div className="list-header">
            <div>
              <h2>Tasks</h2>
              <p>{isLoading ? 'Loading tasks...' : `${tasks.length} active rows`}</p>
            </div>
            <button type="button" className="ghost-action" onClick={loadDashboard}>
              Refresh
            </button>
          </div>

          {!isLoading && tasks.length === 0 && (
            <div className="empty-state">No tasks yet.</div>
          )}

          <div className="task-stack">
            {tasks.map((task) => (
              <article className="task-card" key={task.id}>
                <div className="task-main">
                  <div>
                    <h3>{task.title}</h3>
                    <p>{task.description || 'No description provided.'}</p>
                  </div>
                  <span className={`priority priority-${task.priority.toLowerCase()}`}>
                    {task.priority}
                  </span>
                </div>

                <div className="task-meta">
                  <span
                    className={`status status-${task.status
                      .replace(' ', '-')
                      .toLowerCase()}`}
                  >
                    {task.status}
                  </span>
                  <span>Created {formatDate(task.createdAt)}</span>
                </div>

                <div className="task-actions">
                  <button type="button" onClick={() => cycleStatus(task)}>
                    Next Status
                  </button>
                  <button
                    type="button"
                    className="danger-action"
                    onClick={() => deleteTask(task.id)}
                  >
                    Delete
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
      </section>
    </main>
  )
}

export default App
