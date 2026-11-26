import { useEffect, useState } from 'react'
import { getFlights } from './api'
import { FlightCard } from './FlightCard'
import type { FlightResponse } from './types'
import './App.css'

type LoadStatus = 'loading' | 'error' | 'ready'

function App() {
  // State: data that redraws the screen when it changes
  const [flights, setFlights] = useState<FlightResponse[]>([])
  const [status, setStatus] = useState<LoadStatus>('loading')
  const [search, setSearch] = useState('')

  // Runs after the first render, not during it: load the flights once
  useEffect(() => {
    getFlights()
      .then(page => {
        setFlights(page.items)
        setStatus('ready')
      })
      .catch(() => setStatus('error'))
  }, [])

  // Derived from state: recalculated on every render, never stored on its own
  const text = search.toLowerCase()
  const visibleFlights = flights.filter(f => `${f.origin} ${f.destination}`.toLowerCase().includes(text))
  const totalSeats = flights.reduce((sum, f) => sum + f.availableSeats, 0)

  function reserve(id: number) {
    // Never change state in place, hand React a new one: a new object for this flight, the others stay as they are
    setFlights(current => current.map(f => (f.id === id ? { ...f, availableSeats: f.availableSeats - 1 } : f)))
  }

  if (status === 'loading') {
    return <main><p>Uçuşlar yükleniyor...</p></main>
  }

  if (status === 'error') {
    return <main><p>Uçuşlar yüklenemedi. API çalışıyor mu?</p></main>
  }

  return (
    <main>
      <h1>Uçuşlar</h1>
      <p>
        Toplam boş koltuk: <strong>{totalSeats}</strong>
      </p>
      <input placeholder="Şehir kodu ara (ör. IST)" value={search} onChange={e => setSearch(e.target.value)} />
      {visibleFlights.length === 0 && <p>Aramaya uyan uçuş yok.</p>}
      {visibleFlights.map(f => (
        <FlightCard key={f.id} flight={f} onReserve={reserve} />
      ))}
    </main>
  )
}

export default App
