# Changelog

Tất cả các thay đổi quan trọng của project sẽ được ghi lại trong file này.

Định dạng dựa trên [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
và project tuân theo [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-20

### Added
- Core infrastructure với DbConnectionFactory, DbSession, DbSessionFactory
- IBatis-style SQL mapping với XML configuration
- BaseRepository pattern với Dapper
- SqlMapRepository pattern (IBatis-style)
- Full transaction support với isolation levels
- Dependency Injection configuration extensions
- Multiple database connection support
- Example implementations cho User và Product entities
- Comprehensive documentation bằng tiếng Việt
- Sample console application
- Database schema scripts với sample data
- Support cho stored procedures
- Quick start guide (5 phút)
- IBatis guide chi tiết

### Features
- ✅ .NET 8.0 support
- ✅ Microsoft.Data.SqlClient 5.1.5
- ✅ Dapper 2.1.28 integration
- ✅ Session management giống Hibernate/IBatis
- ✅ XML-based SQL mapping
- ✅ Repository pattern
- ✅ Transaction management
- ✅ Connection pooling
- ✅ Async/await support
- ✅ Strongly-typed parameters

### Documentation
- README.md với full documentation
- QUICK_START.md cho quick start
- docs/IBATIS_GUIDE.md cho IBatis pattern
- Code examples và samples
- Database schema scripts
- API reference documentation

## [Unreleased]

### Planned
- Unit tests với xUnit
- Integration tests
- Performance benchmarks
- Caching support
- Bulk operations support
- Dynamic SQL support
- Result mapping với complex objects
- Multi-tenant support
- Audit logging
- Migration tools

---

## Version History

### Version 1.0.0 (2026-01-20)
Initial release với full IBatis-style SQL mapping support cho .NET 8.

---

**Note**: Để xem chi tiết các commits, vui lòng xem Git history hoặc GitHub releases.
