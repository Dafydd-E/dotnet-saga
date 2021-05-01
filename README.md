# DotNet Saga

A .net 5.0 library to provide a convenient way to sequence together actions with fallback behaviour if an action were to fail.

## Motivation

When designing a codebase using DDD, there are occasions where some events would be better served being handed off to the infrastructure or service layer rather than clogging up a domain entities spcific business logic layer. 

## Future Features

Provide a way of making Saga rollbacks optional instead of required.