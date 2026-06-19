var FirebaseBridge = {
    $FirebaseState: {
        auth: null,
        db: null,
        callbackObj: "BackendRunner",
        isInitialized: false,
        rememberMe: true
    },

    Firebase_Initialize: function(configJson, callbackObjName) {
        var configStr = UTF8ToString(configJson);
        var callbackName = UTF8ToString(callbackObjName);
        FirebaseState.callbackObj = callbackName;
        
        console.log("🔥 [Firebase Bridge] Initializing for object: " + callbackName);
        
        try {
            var config = JSON.parse(configStr);
            
            if (typeof firebase === 'undefined') {
                console.error("❌ [Firebase Bridge] Firebase SDK not found! Check index.html scripts.");
                return;
            }

            if (!firebase.apps.length) {
                firebase.initializeApp(config);
            }
            
            FirebaseState.auth = firebase.auth();
            FirebaseState.auth.setPersistence(firebase.auth.Auth.Persistence.LOCAL).catch(function(error) {
                console.warn("[Firebase Bridge] Failed to set default auth persistence: " + error.message);
            });
            
            if (typeof firebase.firestore === 'function') {
                FirebaseState.db = firebase.firestore();
                FirebaseState.isInitialized = true;
                console.log("🔥 [Firebase Bridge] Initialized successfully with Firestore");
            } else {
                console.error("❌ [Firebase Bridge] Firestore function not found! Make sure firebase-firestore.js is loaded.");
            }
        } catch (e) {
            console.error("❌ [Firebase Bridge] Initialization Error: " + e.message);
        }
    },

    Firebase_SetRememberMe: function(rememberMe) {
        FirebaseState.rememberMe = rememberMe === 1;
    },

    Firebase_TryAutoLogin: function() {
        if (!FirebaseState.auth || !FirebaseState.db) {
            setTimeout(function() {
                SendMessage(FirebaseState.callbackObj, "OnFirebaseAutoLoginFailed", "Firebase is not initialized.");
            }, 0);
            return;
        }

        var unsubscribe = FirebaseState.auth.onAuthStateChanged(function(user) {
            if (unsubscribe) {
                unsubscribe();
            }

            if (!user) {
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseAutoLoginFailed", "No persisted Firebase user.");
                }, 0);
                return;
            }

            FirebaseState.db.collection("users").doc(user.uid).get()
                .then(function(doc) {
                    var userData = {
                        userId: user.uid,
                        email: user.email || "",
                        username: "User",
                        avatar_index: 0,
                        avatar_selected: false
                    };

                    if (doc.exists) {
                        var data = doc.data();
                        userData.username = data.username || userData.username;
                        userData.avatar_index = data.avatar_index || 0;
                        userData.avatar_selected = data.avatar_selected === true || userData.avatar_index > 0;
                    }

                    setTimeout(function() {
                        SendMessage(FirebaseState.callbackObj, "OnFirebaseAutoLoginSuccess", JSON.stringify(userData));
                    }, 0);
                })
                .catch(function(error) {
                    setTimeout(function() {
                        SendMessage(FirebaseState.callbackObj, "OnFirebaseAutoLoginFailed", error.message);
                    }, 0);
                });
        }, function(error) {
            setTimeout(function() {
                SendMessage(FirebaseState.callbackObj, "OnFirebaseAutoLoginFailed", error.message);
            }, 0);
        });
    },

    Firebase_Logout: function() {
        if (!FirebaseState.auth) {
            return;
        }

        FirebaseState.auth.signOut().catch(function(error) {
            console.warn("[Firebase Bridge] Logout failed: " + error.message);
        });
    },

    Firebase_Register: function(email, password, username) {
        if (!FirebaseState.auth) {
            console.error("❌ [Firebase Bridge] Cannot Register: Auth not initialized!");
            return;
        }
        
        var emailStr = UTF8ToString(email);
        var passStr = UTF8ToString(password);
        var userStr = UTF8ToString(username);

        var persistence = FirebaseState.rememberMe
            ? firebase.auth.Auth.Persistence.LOCAL
            : firebase.auth.Auth.Persistence.SESSION;

        FirebaseState.auth.setPersistence(persistence)
            .then(function() {
                return FirebaseState.auth.createUserWithEmailAndPassword(emailStr, passStr);
            })
            .then(function(userCredential) {
                var user = userCredential.user;
                
                // Create user document in Firestore
                return FirebaseState.db.collection("users").doc(user.uid).set({
                    username: userStr,
                    email: emailStr,
                    avatar_index: 0,
                    avatar_selected: false,
                    created_at: firebase.firestore.FieldValue.serverTimestamp()
                }).then(function() {
                    setTimeout(function() {
                        SendMessage(FirebaseState.callbackObj, "OnFirebaseRegisterSuccess", JSON.stringify({
                            userId: user.uid,
                            email: user.email,
                            username: userStr
                        }));
                    }, 0);
                });
            })
            .catch(function(error) {
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseError", error.message);
                }, 0);
            });
    },

    Firebase_Login: function(email, password) {
        if (!FirebaseState.auth) {
            console.error("❌ [Firebase Bridge] Cannot Login: Auth not initialized!");
            return;
        }
        
        var emailStr = UTF8ToString(email);
        var passStr = UTF8ToString(password);

        var persistence = FirebaseState.rememberMe
            ? firebase.auth.Auth.Persistence.LOCAL
            : firebase.auth.Auth.Persistence.SESSION;

        FirebaseState.auth.setPersistence(persistence)
            .then(function() {
                return FirebaseState.auth.signInWithEmailAndPassword(emailStr, passStr);
            })
            .then(function(userCredential) {
                var user = userCredential.user;
                
                // Fetch user data from Firestore
                return FirebaseState.db.collection("users").doc(user.uid).get()
                    .then(function(doc) {
                        var userData = {
                            userId: user.uid,
                            email: user.email,
                            username: "User", // Default
                            avatar_index: 0,
                            avatar_selected: false
                        };
                        
                        if (doc.exists) {
                            var data = doc.data();
                            userData.username = data.username || userData.username;
                            userData.avatar_index = data.avatar_index || 0;
                            userData.avatar_selected = data.avatar_selected === true || userData.avatar_index > 0;
                        }
                        
                        setTimeout(function() {
                            SendMessage(FirebaseState.callbackObj, "OnFirebaseLoginSuccess", JSON.stringify(userData));
                        }, 0);
                    });
            })
            .catch(function(error) {
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseError", error.message);
                }, 0);
            });
    },

    // ====================== GENERIC FIRESTORE FUNCTIONS ======================
    // path: "collection/doc" or "collection/doc/subcollection/subdoc"
    
    Firebase_Firestore_Set: function(path, dataJson) {
        if (!FirebaseState.db) return;
        var pathStr = UTF8ToString(path);
        var data = JSON.parse(UTF8ToString(dataJson));
        
        var parts = pathStr.split('/');
        var docRef = FirebaseState.db;
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) docRef = docRef.collection(parts[i]);
            else docRef = docRef.doc(parts[i]);
        }

        docRef.set(data)
            .then(function() {
                setTimeout(function() { SendMessage(FirebaseState.callbackObj, "OnFirebaseGenericSuccess", pathStr); }, 0);
            })
            .catch(function(error) {
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseGenericError", JSON.stringify({
                        path: pathStr,
                        message: error.message
                    }));
                }, 0);
            });
    },

    Firebase_Firestore_Update: function(path, dataJson) {
        if (!FirebaseState.db) return;
        var pathStr = UTF8ToString(path);
        var data = JSON.parse(UTF8ToString(dataJson));
        
        var parts = pathStr.split('/');
        var docRef = FirebaseState.db;
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) docRef = docRef.collection(parts[i]);
            else docRef = docRef.doc(parts[i]);
        }

        docRef.update(data)
            .then(function() {
                setTimeout(function() { SendMessage(FirebaseState.callbackObj, "OnFirebaseGenericSuccess", pathStr); }, 0);
            })
            .catch(function(error) {
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseGenericError", JSON.stringify({
                        path: pathStr,
                        message: error.message
                    }));
                }, 0);
            });
    },

    Firebase_Firestore_Get: function(path) {
        if (!FirebaseState.db) return;
        var pathStr = UTF8ToString(path);
        
        var parts = pathStr.split('/');
        var docRef = FirebaseState.db;
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) docRef = docRef.collection(parts[i]);
            else docRef = docRef.doc(parts[i]);
        }

        docRef.get()
            .then(function(doc) {
                setTimeout(function() {
                    if (doc.exists) {
                        SendMessage(FirebaseState.callbackObj, "OnFirebaseGenericDataSuccess", JSON.stringify({
                            path: pathStr,
                            data: JSON.stringify(doc.data())
                        }));
                    } else {
                        SendMessage(FirebaseState.callbackObj, "OnFirebaseError", "Document not found at " + pathStr);
                    }
                }, 0);
            })
            .catch(function(error) {
                setTimeout(function() { SendMessage(FirebaseState.callbackObj, "OnFirebaseError", error.message); }, 0);
            });
    },

    Firebase_Firestore_GetCollection: function(path) {
        if (!FirebaseState.db) return;
        var pathStr = UTF8ToString(path);
        
        var parts = pathStr.split('/');
        var ref = FirebaseState.db;
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) ref = ref.collection(parts[i]);
            else ref = ref.doc(parts[i]);
        }

        // Ensure we ended on a collection
        if (parts.length % 2 === 0) {
            console.error("❌ [Firebase Bridge] GetCollection requires a path to a collection, not a document: " + pathStr);
            return;
        }

        ref.get()
            .then(function(querySnapshot) {
                var results = [];
                querySnapshot.forEach(function(doc) {
                    results.push({
                        id: doc.id,
                        data: JSON.stringify(doc.data())
                    });
                });
                
                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseCollectionSuccess", JSON.stringify({
                        path: pathStr,
                        requestKey: pathStr,
                        items: results
                    }));
                }, 0);
            })
            .catch(function(error) {
                setTimeout(function() { SendMessage(FirebaseState.callbackObj, "OnFirebaseError", error.message); }, 0);
            });
    },

    Firebase_Firestore_GetCollectionOrdered: function(path, orderByField, descending, limit) {
        if (!FirebaseState.db) return;
        var pathStr = UTF8ToString(path);
        var orderFieldStr = UTF8ToString(orderByField);
        var direction = descending ? "desc" : "asc";
        var safeLimit = Math.max(1, Math.min(limit || 50, 100));
        var requestKey = pathStr + "|" + orderFieldStr + "|" + direction + "|" + safeLimit;

        var parts = pathStr.split('/');
        var ref = FirebaseState.db;
        for (var i = 0; i < parts.length; i++) {
            if (i % 2 === 0) ref = ref.collection(parts[i]);
            else ref = ref.doc(parts[i]);
        }

        // Ensure we ended on a collection
        if (parts.length % 2 === 0) {
            console.error("âŒ [Firebase Bridge] GetCollectionOrdered requires a path to a collection, not a document: " + pathStr);
            return;
        }

        ref.orderBy(orderFieldStr, direction).limit(safeLimit).get()
            .then(function(querySnapshot) {
                var results = [];
                querySnapshot.forEach(function(doc) {
                    results.push({
                        id: doc.id,
                        data: JSON.stringify(doc.data())
                    });
                });

                setTimeout(function() {
                    SendMessage(FirebaseState.callbackObj, "OnFirebaseCollectionSuccess", JSON.stringify({
                        path: pathStr,
                        requestKey: requestKey,
                        items: results
                    }));
                }, 0);
            })
            .catch(function(error) {
                setTimeout(function() { SendMessage(FirebaseState.callbackObj, "OnFirebaseError", error.message); }, 0);
            });
    }
};

autoAddDeps(FirebaseBridge, '$FirebaseState');
mergeInto(LibraryManager.library, FirebaseBridge);
