#!/usr/bin/env python3
"""Keep a Dragino -NB node's modem continuously powered and poll until it recovers.

This is the decisive experiment: does the back-off advance in wall-clock time, or only
while the modem is powered? Time-to-recovery under continuous power is the answer.
  - recovers in a few hours  -> duty-cycle model (the sleep cycle stretches it)
  - recovers ~when it would have anyway -> wall-clock model

Field durations to compare against: 166.3, 172.1, 196.0, 200.1, 226.1 h.
See InternalDocs/runbooks/capture-blocked-nbiot-node.md
"""
import argparse, datetime, time
import serial

POLL = [("AT+CGATT?", 10), ("AT+CGACT?", 10), ("AT+QESMC?", 6), ("AT+CEREG?", 5)]

def now():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

def ask(ser, cmd, wait):
    ser.reset_input_buffer()
    ser.write((cmd + "\r\n").encode())
    end, buf = time.time() + wait, b""
    while time.time() < end:
        c = ser.read(ser.in_waiting or 1)
        if c:
            buf += c
            t = buf.decode("utf-8", "replace").rstrip()
            if t.endswith(("OK", "ERROR")) or "+CME ERROR" in t:
                break
        else:
            time.sleep(0.05)
    return " | ".join(l.strip() for l in buf.decode("utf-8", "replace").splitlines()
                      if l.strip() and l.strip() != cmd)

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--port", default="/dev/ttyUSB0")
    p.add_argument("--baud", type=int, default=9600)
    p.add_argument("--password", default="12345678")
    p.add_argument("--interval", type=int, default=60, help="seconds between polls")
    p.add_argument("--log", default=None)
    a = p.parse_args()

    logname = a.log or f"nbiot-poll-{datetime.datetime.now(datetime.timezone.utc):%Y%m%dT%H%M%SZ}.log"
    log = open(logname, "w", buffering=1)
    def emit(s):
        print(s, flush=True); log.write(s + "\n")

    ser = serial.Serial(a.port, a.baud, bytesize=8, parity="N", stopbits=1, timeout=0.2)
    time.sleep(0.3); ser.reset_input_buffer()
    if a.password:
        ser.write((a.password + "\r\n").encode()); time.sleep(1); ser.reset_input_buffer()
    ser.write(b"ATE1\r\n"); time.sleep(0.5); ser.reset_input_buffer()

    emit(f"# poll started {now()}  interval={a.interval}s  — keeping modem powered (CFUN=1)")
    emit(ask(ser, "AT+CFUN=1", 25))
    start = time.time()
    attached_since = None
    while True:
        line = [now(), f"t+{(time.time()-start)/3600:6.3f}h"]
        for cmd, wait in POLL:
            line.append(f"{cmd} -> {ask(ser, cmd, wait)}")
        s = "  ".join(line)
        emit(s)
        # crude recovery detector: an active PDP context
        if "+CGACT: 1,1" in s:
            if attached_since is None:
                attached_since = time.time()
                emit(f"### PDP CONTEXT ACTIVE at {now()} — "
                     f"{(attached_since-start)/3600:.3f} h after continuous power began")
        time.sleep(a.interval)

if __name__ == "__main__":
    main()
