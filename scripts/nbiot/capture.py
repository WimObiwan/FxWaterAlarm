#!/usr/bin/env python3
"""One-shot diagnostic capture from a Dragino -NB node over UART (9600 8N1).

READ-ONLY: sends nothing that changes state. Run this BEFORE any repair attempt.
See InternalDocs/runbooks/capture-blocked-nbiot-node.md
"""
import argparse, datetime, sys, time
import serial

# (command, seconds to wait) -- timeouts from the BC660K AT manual's "Maximum Response Time"
COMMANDS = [
    ("AT",            3),
    ("ATE1",          3),
    ("AT+QESMC?",     6),   # <-- THE one: ESM reject cause (T3396 trigger)
    ("AT+QEMMS?",     6),   # EMM main/sub state
    ("AT+QEMMTIMER?", 6),   # T3346/T3448/T3412 (NOT T3396 -- empty here is expected)
    ("AT+CEREG=5",    5),
    ("AT+CEREG?",     5),   # registration + <cause_type>,<reject_cause>
    ("AT+CGATT?",     10),
    ("AT+CGACT?",     10),
    ("AT+CGPADDR",    10),
    ("AT+CGDCONT?",   10),
    ("AT+COPS?",      20),  # which PLMN -- back-off is per-PLMN
    ("AT+QENG=3",     20),
    ("AT+QENG=0",     20),
    ("AT+CESQ",       5),
    ("AT+CSQ",        5),
    ("AT+QCCID",      5),
    ("AT+CIMI",       5),
    ("AT+CGSN=1",     5),
    ("AT+CGMR",       5),
    ("AT+CPIN?",      5),
    ("AT+QBAND?",     5),
    ("AT+CPSMS?",     5),
    ("AT+CFG",        15),  # node-side config (TDC, CSQTIME, server)
]

def now():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ")

def drain(ser, seconds, out):
    """Read for `seconds`, returning early once OK/ERROR seen at a line end."""
    end = time.time() + seconds
    buf = b""
    while time.time() < end:
        chunk = ser.read(ser.in_waiting or 1)
        if chunk:
            buf += chunk
            tail = buf.decode("utf-8", "replace").rstrip()
            if tail.endswith(("OK", "ERROR")) or "+CME ERROR" in tail:
                break
        else:
            time.sleep(0.05)
    return buf.decode("utf-8", "replace")

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--port", default="/dev/ttyUSB0")
    p.add_argument("--baud", type=int, default=9600)
    p.add_argument("--password", default="12345678", help="node console password ('' to skip)")
    p.add_argument("--log", default=None)
    a = p.parse_args()

    logname = a.log or f"nbiot-capture-{datetime.datetime.now(datetime.timezone.utc):%Y%m%dT%H%M%SZ}.log"
    log = open(logname, "w", buffering=1)

    def emit(s):
        print(s)
        log.write(s + "\n")

    emit(f"# capture started {now()}  port={a.port} baud={a.baud}")
    ser = serial.Serial(a.port, a.baud, bytesize=8, parity="N", stopbits=1, timeout=0.2)
    time.sleep(0.3)
    ser.reset_input_buffer()

    if a.password:
        emit(f"\n[{now()}] >>> <password>")
        ser.write((a.password + "\r\n").encode())
        emit(drain(ser, 4, log).strip())

    for cmd, wait in COMMANDS:
        emit(f"\n[{now()}] >>> {cmd}")
        ser.reset_input_buffer()
        ser.write((cmd + "\r\n").encode())
        resp = drain(ser, wait, log)
        emit(resp.strip() or "(no response)")

    emit(f"\n# capture finished {now()}")
    ser.close()
    log.close()
    print(f"\nSaved to {logname}", file=sys.stderr)

if __name__ == "__main__":
    main()
