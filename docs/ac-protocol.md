# AC Dedicated Server UDP Protocol Reference

Port: 9996 (configurable via `AcServer:UdpPort` in appsettings.json)
Encoding: Binary, little-endian, strings are length-prefixed UTF-32LE (wide) or UTF-8
Reference: https://github.com/mathiasuk/ac-pserver (Python 3 rewrite of Kunos example)

## Server → Plugin Packets

### NewSession (50) / SessionInfo (59)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 50 or 59 |
| protocol_version | byte | Currently 4 |
| session_index | byte | |
| current_session_index | byte | |
| session_count | byte | |
| server_name | stringW | UTF-32LE |
| track | string | UTF-8 |
| track_config | string | UTF-8 |
| name | string | Session name |
| type | byte | 1=Practice, 2=Qualifying, 3=Race |
| time | uint16 | Session time |
| laps | uint16 | 0 = unlimited |
| wait_time | uint16 | |
| ambient_temp | byte | Celsius |
| road_temp | byte | Celsius |
| weather_graphics | string | UTF-8 |
| elapsed_ms | int32 | |

### NewConnection (51)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 51 |
| driver_name | stringW | UTF-32LE |
| driver_guid | stringW | UTF-32LE |
| car_id | byte | |
| car_model | string | UTF-8 |
| car_skin | string | UTF-8 |

### ConnectionClosed (52)
Same format as NewConnection but packet_type = 52.

### CarUpdate (53)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 53 |
| car_id | byte | |
| position | vec3f | 3x float (x, y, z) |
| velocity | vec3f | 3x float (x, y, z) |
| gear | byte | |
| engine_rpm | uint16 | |
| normalized_spline_pos | float | 0.0 - 1.0 |

### CarInfo (54) - response to GetCarInfo
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 54 |
| car_id | byte | |
| is_connected | byte | boolean |
| car_model | stringW | UTF-32LE |
| car_skin | stringW | UTF-32LE |
| driver_name | stringW | UTF-32LE |
| driver_team | stringW | UTF-32LE |
| driver_guid | stringW | UTF-32LE |

### EndSession (55)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 55 |
| filename | stringW | Report filename |

### ClientLoaded (58)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 58 |
| car_id | byte | |

### LapCompleted (73)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 73 |
| car_id | byte | |
| laptime | uint32 | milliseconds |
| cuts | byte | track limit violations |
| cars_count | byte | |
| **leaderboard** (repeated cars_count times): | | |
| - car_id | byte | |
| - time | uint32 | best lap ms |
| - laps | byte | laps completed |
| grip_level | byte | |

### ClientEvent (130)
| Field | Type | Notes |
|-------|------|-------|
| packet_type | byte | 130 |
| event_type | byte | 10=car collision, 11=env collision |
| car_id | byte | |
| other_car_id | byte | only if event_type=10 |
| impact_speed | float | |
| world_pos | vec3f | |
| rel_pos | vec3f | |

## Plugin → Server Packets

| Type | Name | Fields |
|------|------|--------|
| 200 | RealtimePosInterval | interval (uint16 ms) |
| 201 | GetCarInfo | car_id (byte) |
| 202 | SendChat | car_id (byte), msg (stringW) |
| 203 | BroadcastChat | msg (stringW) |
| 204 | GetSessionInfo | (empty or session index) |
| 205 | SetSessionInfo | session config fields |
| 206 | KickUser | car_id (byte) |
| 207 | NextSession | (empty) |
| 208 | RestartSession | (empty) |
| 209 | AdminCommand | command (stringW) |

## String Encoding
- **stringW**: byte (char count) + N×4 bytes UTF-32LE
- **string**: byte (byte count) + N bytes UTF-8

## Notes
- Vanilla server has no sector/split times (requires AC Server Manager for LAP_SPLIT type 150)
- All multi-byte integers are little-endian
