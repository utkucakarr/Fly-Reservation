import { useState } from 'react'
import { sampleFlights } from './data'
import { FlightCard } from './FlightCard'
import './App.css'

function App() {
  // State: data that redraws the screen when it changes
  const [flights, setFlights] = useState(sampleFlights)
  const [search, setSearch] = useState('')

  // Derived from state: recalculated on every render, never stored on its own
  const text = search.toLowerCase()
  const visibleFlights = flights.filter(f => `${f.origin} ${f.destination}`.toLowerCase().includes(text))
  const totalSeats = flights.reduce((sum, f) => sum + f.availableSeats, 0)

  function reserve(id: number) {
    const flight = flights.find(f => f.id === id)!
    flight.availableSeats--
    setFlights(flights)
  }

  return (
    <main>
      <h1>Uçuşlar</h1>
      <p>
        Toplam boş koltuk: <strong>{totalSeats}</strong>
      </p>
      <input placeholder="Şehir ara (ör. Ankara)" value={search} onChange={e => setSearch(e.target.value)} />
      {visibleFlights.map((f, index) => (
        <FlightCard key={index} flight={f} onReserve={reserve} />
      ))}
    </main>
  )
}

export default App