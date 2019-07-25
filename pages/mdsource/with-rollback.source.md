# With Rollback

Multiple actions a targeted to the same database and changes are rolled back instead of being committed.

Characteristics:

 * Better performance that the standard API.
 * If a test fails, it is not possible to look at the current database state.

If a test has failed, to debug the database state, temporarily switch back to the standard API.


## Usage

snippet: WithRollback


## EntityFramework Usage

snippet: EfWithRollback