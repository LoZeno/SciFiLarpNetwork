export const fictionalCode = `use std::collections::{HashMap, VecDeque};
use std::time::{Duration, Instant};

#[derive(Clone, Debug)]
struct RelaySession {
    station: String,
    nonce: [u8; 32],
    issued_at: Instant,
    expires_after: Duration,
    attempts: u16,
}

impl RelaySession {
    fn is_current(&self, now: Instant) -> bool {
        now.saturating_duration_since(self.issued_at) < self.expires_after
    }

    fn verify_route(&self, route: &RouteFrame, registry: &Registry) -> bool {
        let Some(node) = registry.nodes.get(&route.origin) else {
            return false;
        };
        node.enabled
            && node.channel == route.channel
            && route.sequence == self.attempts as u64
            && route.payload.len() <= registry.max_frame_size
    }
}

#[derive(Default)]
struct Registry {
    nodes: HashMap<String, RelayNode>,
    max_frame_size: usize,
}

struct RelayNode {
    enabled: bool,
    channel: u16,
}

struct RouteFrame {
    origin: String,
    channel: u16,
    sequence: u64,
    payload: Vec<u8>,
}

enum SignalState {
    Standby,
    Calibrating { progress: u8 },
    Connected(RelaySession),
    Rejected { reason: &'static str },
}

fn evaluate_signal(
    session: &RelaySession,
    frame: &RouteFrame,
    registry: &Registry,
    now: Instant,
) -> SignalState {
    if !session.is_current(now) {
        return SignalState::Rejected { reason: "expired" };
    }

    if !session.verify_route(frame, registry) {
        return SignalState::Rejected { reason: "route mismatch" };
    }

    let remaining = session.expires_after
        .saturating_sub(now.saturating_duration_since(session.issued_at));
    if remaining < Duration::from_secs(2) {
        return SignalState::Calibrating { progress: 96 };
    }

    SignalState::Connected(session.clone())
}

fn consume_queue(
    queue: &mut VecDeque<RouteFrame>,
    registry: &Registry,
    session: &RelaySession,
    now: Instant,
) -> Vec<SignalState> {
    let mut states = Vec::with_capacity(queue.len());
    while let Some(frame) = queue.pop_front() {
        states.push(evaluate_signal(session, &frame, registry, now));
    }
    states
}`;
