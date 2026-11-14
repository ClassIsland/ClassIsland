import Foundation

@main
struct ClassIslandSystemSnapshotTests {
    private enum Failure: Error {
        case assertion(String)
    }

    static func main() throws {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(secondsFromGMT: 0)!
        let now = Date(timeIntervalSince1970: 1_800_000_000)
        let lesson = makeLesson(id: "first", start: now.addingTimeInterval(60), end: now.addingTimeInterval(600))
        let next = makeLesson(id: "second", start: now.addingTimeInterval(900), end: now.addingTimeInterval(1_500))
        let tomorrow = makeLesson(id: "tomorrow", start: now.addingTimeInterval(90_000), end: now.addingTimeInterval(91_000))
        let snapshot = makeSnapshot(now: now, lessons: [lesson, next, tomorrow])
        try snapshot.validate()
        try expect(snapshot.isCurrent(at: now), "Fresh snapshots must be available.")
        try expect(!snapshot.isCurrent(at: now.addingTimeInterval(172_800)), "Snapshots expire at validUntil.")
        try expect(!snapshot.isCurrent(at: now.addingTimeInterval(-301)), "Future snapshots must not be used after clock changes.")
        try expect(snapshot.currentOrNextLesson(at: now)?.id == lesson.id, "Show the next lesson before its start.")
        try expect(snapshot.currentOrNextLesson(at: lesson.startDate)?.id == lesson.id, "Show the current lesson at its start.")
        try expect(snapshot.currentOrNextLesson(at: lesson.endDate)?.id == next.id, "Advance at the exact end of a lesson.")
        try expect(snapshot.lessons(on: now, calendar: calendar).count == 2, "Today must exclude tomorrow's lessons.")
        let midnight = calendar.nextDate(after: now, matching: DateComponents(hour: 0, minute: 0), matchingPolicy: .nextTime)!
        let dates = snapshot.timelineDates(after: now, calendar: calendar)
        try expect(dates.contains(lesson.startDate) && dates.contains(lesson.endDate), "Timeline must include lesson boundaries.")
        try expect(dates.contains(midnight), "Timeline must update its day at midnight.")
        try expect(dates.last == Date(timeIntervalSince1970: snapshot.validUntil), "Timeline must include expiry even if reload is delayed.")
        try expect(!dates.contains(tomorrow.startDate), "Normal timeline entries are bounded to one day.")
        try expect(snapshot.timelineDates(after: now.addingTimeInterval(172_800)) == [now.addingTimeInterval(172_800)], "Expired snapshots must not schedule stale lessons.")

        try expectInvalid(makeSnapshot(now: now, lessons: [lesson, lesson]), "Duplicate lesson identifiers must be rejected.")
        try expectInvalid(makeSnapshot(now: now, lessons: [next, lesson]), "Out-of-order lessons must be rejected.")
        try expectInvalid(makeSnapshot(now: now, lessons: [makeLesson(id: "bad", start: now, end: now)]), "Empty lesson intervals must be rejected.")
        let unsafeLesson = ClassIslandSystemLesson(
            id: "unsafe", subject: "数学", location: "", startAt: now.timeIntervalSince1970,
            endAt: now.addingTimeInterval(60).timeIntervalSince1970, url: "https://example.com")
        try expectInvalid(makeSnapshot(now: now, lessons: [unsafeLesson]), "System entries must only link to the app.")
        let encoded = try JSONEncoder().encode(snapshot)
        let decoded = try JSONDecoder().decode(ClassIslandSystemSnapshot.self, from: encoded)
        try expect(decoded.lessons == snapshot.lessons, "Snapshot JSON must preserve lesson identifiers and timestamps.")

        let today = calendar.startOfDay(for: now)
        let day = { (offset: Int) in calendar.date(byAdding: .day, value: offset, to: today)! }
        let store = ClassIslandWidgetDayStore.self
        let tomorrowText = store.shifted(stored: nil, by: 1, today: now, calendar: calendar)
        try expect(store.resolve(stored: tomorrowText, today: now, calendar: calendar) == day(1), "Widget can move to the next day.")
        try expect(store.shifted(stored: nil, by: -1, today: now, calendar: calendar) == nil, "Widget cannot move before today.")
        try expect(store.shifted(stored: tomorrowText, by: 0, today: now, calendar: calendar) == nil, "Offset 0 returns to today.")
        let lastText = store.shifted(stored: nil, by: 20, today: now, calendar: calendar)
        try expect(store.resolve(stored: lastText, today: now, calendar: calendar) == day(6), "Widget stays within the 7-day snapshot.")
        try expect(store.resolve(stored: tomorrowText, today: day(2), calendar: calendar) == day(2), "Past selections fall back to today.")
        try expect(store.resolve(stored: "invalid", today: now, calendar: calendar) == today, "Invalid selections fall back to today.")
        print("ClassIsland system snapshot: 23 checks passed.")
    }

    private static func makeLesson(id: String, start: Date, end: Date) -> ClassIslandSystemLesson {
        ClassIslandSystemLesson(
            id: id, subject: "数学", location: "302", startAt: start.timeIntervalSince1970,
            endAt: end.timeIntervalSince1970, url: "classisland://app/system/schedule?lesson=\(id)")
    }

    private static func makeSnapshot(now: Date, lessons: [ClassIslandSystemLesson]) -> ClassIslandSystemSnapshot {
        ClassIslandSystemSnapshot(
            generatedAt: now.timeIntervalSince1970, validUntil: now.addingTimeInterval(172_800).timeIntervalSince1970,
            notificationsEnabled: true, lessons: lessons, classPlans: [])
    }

    private static func expect(_ condition: Bool, _ message: String) throws {
        if !condition { throw Failure.assertion(message) }
    }

    private static func expectInvalid(_ snapshot: ClassIslandSystemSnapshot, _ message: String) throws {
        do {
            try snapshot.validate()
        } catch {
            return
        }
        throw Failure.assertion(message)
    }
}
