FROM quay.io/keycloak/keycloak:23.0
COPY keycloak.conf /opt/keycloak/conf/keycloak.conf
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start", "--http-enabled=true", "--hostname-strict=false", "--proxy=edge", "--http-port=10000"]