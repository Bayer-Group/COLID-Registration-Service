# Functional Tests

## Setup

The graphs within the setup folder are loaded into an in-memory triplestore at the beginning of the tests.
If graphs have to be added or changed, the mapping between *.ttl file and graph name must be done via appsettings.Testing.json.

## Writing an Functional Test

For each API controller of COLID there should be an Functional test class, which covers all functions with different positive and negative scenarios of the API controller.
The Functional Test classes should be stored within the folder Controller and named as follows: `<ControllerName>ControllerTests.cs`

The tests within the test classes should check several sub parts of the data returned by the API when checking the assertions and not load the data via JSON files and compare them completely.

When storing data in the In-Memory Triplestore, the tests should not touch data that was already present when the Functional Tests were started.
This allows the Functional tests to work independently of each other.
Generated test data stored in the Triplestore should always select a prefix that matches the test, so that its creation origin can be assigned.

The names of the test methods should be appropriate to the current test plan and should contain a suffix at the end with information whether the test plan should be successful or, for example, should become an exception, for example:

* \<test plan name\>_successful
* \<test plan name\>_ThrowsArgumentException
